using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;
using Wfm.DemandModule.Infrastructure.Services;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/streams/{streamId:guid}/mappings")]
public sealed class MappingsController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly IAuditWriter _audit;

    public MappingsController(DemandDbContext db, IAuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    public sealed record CreateMappingRequest(string Name, List<RuleDto> Rules);

    public sealed record RuleDto(
        string Name,
        string EventType,
        string? ConditionExpression,
        int SortOrder,
        List<RuleActivityDto> Activities
    );

    public sealed record RuleActivityDto(
        Guid ActivityId,
        decimal BaseHours,
        string? UnitExpression,
        decimal PerUnitHours,
        string? MultiplierExpression
    );

    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<object>> GetVersions(Guid streamId, CancellationToken ct)
    {
        var versions = await _db.MappingVersions.AsNoTracking()
            .Where(x => x.StreamId == streamId)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(ct);

        return Ok(new { versions });
    }

    [HttpGet("active")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<object>> GetActive(Guid streamId, CancellationToken ct)
    {
        var active = await _db.MappingVersions.AsNoTracking()
            .Where(x => x.StreamId == streamId && x.IsActive && !x.IsArchived)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (active is null) return NotFound();

        var rules = await _db.MappingRules.AsNoTracking().Where(r => r.MappingVersionId == active.Id).ToListAsync(ct);
        var ruleIds = rules.Select(r => r.Id).ToList();
        var ras = await _db.MappingRuleActivities.AsNoTracking().Where(a => ruleIds.Contains(a.MappingRuleId)).ToListAsync(ct);

        return Ok(new { version = active, rules, ruleActivities = ras });
    }

    [HttpGet("latest")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<object>> GetLatest(Guid streamId, CancellationToken ct)
    {
        var mv = await _db.MappingVersions.AsNoTracking()
            .Where(x => x.StreamId == streamId && !x.IsArchived)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (mv is null) return NotFound();

        var rules = await _db.MappingRules.AsNoTracking().Where(r => r.MappingVersionId == mv.Id).ToListAsync(ct);
        var ruleIds = rules.Select(r => r.Id).ToList();
        var ras = await _db.MappingRuleActivities.AsNoTracking().Where(a => ruleIds.Contains(a.MappingRuleId)).ToListAsync(ct);

        return Ok(new { version = mv, rules, ruleActivities = ras });
    }

    [HttpPost]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> Create(Guid streamId, [FromBody] CreateMappingRequest req, CancellationToken ct)
    {
        if (!await _db.DataStreams.AnyAsync(x => x.Id == streamId, ct))
            return NotFound(new { message = "Stream not found" });

        var nextVersion = (await _db.MappingVersions.Where(x => x.StreamId == streamId)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;

        var mv = new MappingVersion
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            VersionNumber = nextVersion,
            Name = req.Name,
            CreatedByUserId = UserId(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = !await _db.MappingVersions.AnyAsync(x => x.StreamId == streamId && x.IsActive && !x.IsArchived, ct),
            IsArchived = false
        };

        _db.MappingVersions.Add(mv);

        foreach (var r in req.Rules)
        {
            var rule = new MappingRule
            {
                Id = Guid.NewGuid(),
                MappingVersionId = mv.Id,
                Name = r.Name,
                EventType = r.EventType,
                ConditionExpression = r.ConditionExpression,
                SortOrder = r.SortOrder
            };
            _db.MappingRules.Add(rule);

            foreach (var a in r.Activities)
            {
                var ra = new MappingRuleActivity
                {
                    Id = Guid.NewGuid(),
                    MappingRuleId = rule.Id,
                    ActivityId = a.ActivityId,
                    BaseHours = a.BaseHours,
                    UnitExpression = a.UnitExpression,
                    PerUnitHours = a.PerUnitHours,
                    MultiplierExpression = a.MultiplierExpression
                };
                _db.MappingRuleActivities.Add(ra);

                _db.CalibrationProfiles.Add(new CalibrationProfile
                {
                    Id = Guid.NewGuid(),
                    MappingVersionId = mv.Id,
                    RuleActivityId = ra.Id,
                    Factor = 1.0m,
                    Lambda = 0.1m,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(UserId(), Role(), "MappingCreated", "MappingVersion", mv.Id.ToString(),
            new { mv.VersionNumber, mv.Name, Rules = req.Rules.Count }, ct);

        return Ok(new { mappingVersion = mv });
    }

    [HttpPost("{versionId:guid}/activate")]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> Activate(Guid streamId, Guid versionId, CancellationToken ct)
    {
        var versions = await _db.MappingVersions
            .Where(x => x.StreamId == streamId && !x.IsArchived)
            .ToListAsync(ct);

        var target = versions.FirstOrDefault(x => x.Id == versionId);
        if (target is null) return NotFound();

        foreach (var version in versions)
        {
            version.IsActive = version.Id == target.Id;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(UserId(), Role(), "MappingActivated", "MappingVersion", target.Id.ToString(),
            new { target.VersionNumber, target.Name }, ct);

        return Ok(new { mappingVersion = target });
    }

    [HttpPost("{versionId:guid}/rollback")]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> Rollback(Guid streamId, Guid versionId, CancellationToken ct)
    {
        var target = await _db.MappingVersions.FirstOrDefaultAsync(x => x.StreamId == streamId && x.Id == versionId, ct);
        if (target is null) return NotFound();

        var rules = await _db.MappingRules.Where(r => r.MappingVersionId == target.Id).ToListAsync(ct);
        var ras = await _db.MappingRuleActivities.Where(a => rules.Select(r => r.Id).Contains(a.MappingRuleId)).ToListAsync(ct);

        var nextVersion = (await _db.MappingVersions.Where(x => x.StreamId == streamId)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;

        var mv = new MappingVersion
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            VersionNumber = nextVersion,
            Name = $"Rollback to v{target.VersionNumber}: {target.Name}",
            CreatedByUserId = UserId(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var currentVersions = await _db.MappingVersions.Where(x => x.StreamId == streamId && x.IsActive).ToListAsync(ct);
        foreach (var version in currentVersions)
        {
            version.IsActive = false;
        }

        _db.MappingVersions.Add(mv);

        var ruleMap = new Dictionary<Guid, Guid>();
        foreach (var r in rules)
        {
            var nr = new MappingRule
            {
                Id = Guid.NewGuid(),
                MappingVersionId = mv.Id,
                Name = r.Name,
                EventType = r.EventType,
                ConditionExpression = r.ConditionExpression,
                SortOrder = r.SortOrder
            };
            _db.MappingRules.Add(nr);
            ruleMap[r.Id] = nr.Id;
        }

        foreach (var a in ras)
        {
            var na = new MappingRuleActivity
            {
                Id = Guid.NewGuid(),
                MappingRuleId = ruleMap[a.MappingRuleId],
                ActivityId = a.ActivityId,
                BaseHours = a.BaseHours,
                UnitExpression = a.UnitExpression,
                PerUnitHours = a.PerUnitHours,
                MultiplierExpression = a.MultiplierExpression
            };
            _db.MappingRuleActivities.Add(na);

            _db.CalibrationProfiles.Add(new CalibrationProfile
            {
                Id = Guid.NewGuid(),
                MappingVersionId = mv.Id,
                RuleActivityId = na.Id,
                Factor = 1.0m,
                Lambda = 0.1m,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(UserId(), Role(), "MappingRollback", "MappingVersion", mv.Id.ToString(),
            new { RolledBackTo = target.VersionNumber }, ct);

        return Ok(new { mappingVersion = mv });
    }

    private string UserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    private string Role() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown";
}
