using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Engine;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly CalibrationService _calibration;

    public FeedbackController(DemandDbContext db, CalibrationService calibration)
    {
        _db = db;
        _calibration = calibration;
    }

    public sealed record CreateFeedbackRequest(
        Guid MappingVersionId,
        Guid RuleActivityId,
        DateTime IntervalStartUtc,
        decimal ActualHours,
        decimal? BaseHours,
        string? Comment
    );

    public sealed record FeedbackEntryDto(
        Guid Id,
        Guid MappingVersionId,
        string MappingVersionName,
        Guid RuleActivityId,
        string ActivityName,
        DateTime IntervalStartUtc,
        decimal ActualHours,
        string? Comment,
        string CreatedByUserId,
        DateTime CreatedAtUtc
    );

    public sealed record CalibrationProfileDto(
        Guid Id,
        Guid MappingVersionId,
        string MappingVersionName,
        Guid RuleActivityId,
        string ActivityName,
        decimal Factor,
        decimal Lambda,
        DateTime UpdatedAtUtc
    );

    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<object>> Get([FromQuery] Guid? mappingVersionId, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var safeTake = Math.Clamp(take, 1, 100);

        var entriesQuery =
            from entry in _db.FeedbackEntries.AsNoTracking()
            join version in _db.MappingVersions.AsNoTracking() on entry.MappingVersionId equals version.Id
            join ruleActivity in _db.MappingRuleActivities.AsNoTracking() on entry.RuleActivityId equals ruleActivity.Id
            join activity in _db.WorkActivities.AsNoTracking() on ruleActivity.ActivityId equals activity.Id
            orderby entry.CreatedAtUtc descending
            select new FeedbackEntryDto(
                entry.Id,
                entry.MappingVersionId,
                version.Name,
                entry.RuleActivityId,
                activity.Name,
                entry.IntervalStartUtc,
                entry.ActualHours,
                entry.Comment,
                entry.CreatedByUserId,
                entry.CreatedAtUtc);

        if (mappingVersionId.HasValue)
        {
            entriesQuery = entriesQuery.Where(x => x.MappingVersionId == mappingVersionId.Value);
        }

        var profilesQuery =
            from profile in _db.CalibrationProfiles.AsNoTracking()
            join version in _db.MappingVersions.AsNoTracking() on profile.MappingVersionId equals version.Id
            join ruleActivity in _db.MappingRuleActivities.AsNoTracking() on profile.RuleActivityId equals ruleActivity.Id
            join activity in _db.WorkActivities.AsNoTracking() on ruleActivity.ActivityId equals activity.Id
            orderby profile.UpdatedAtUtc descending
            select new CalibrationProfileDto(
                profile.Id,
                profile.MappingVersionId,
                version.Name,
                profile.RuleActivityId,
                activity.Name,
                profile.Factor,
                profile.Lambda,
                profile.UpdatedAtUtc);

        if (mappingVersionId.HasValue)
        {
            profilesQuery = profilesQuery.Where(x => x.MappingVersionId == mappingVersionId.Value);
        }

        var entries = await entriesQuery.Take(safeTake).ToListAsync(ct);
        var profiles = await profilesQuery.Take(safeTake).ToListAsync(ct);

        return Ok(new
        {
            entries,
            profiles
        });
    }

    [HttpPost]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> Create([FromBody] CreateFeedbackRequest req, CancellationToken ct)
    {
        var profile = await _db.CalibrationProfiles.FirstOrDefaultAsync(x =>
            x.MappingVersionId == req.MappingVersionId && x.RuleActivityId == req.RuleActivityId, ct);

        if (profile is null) return NotFound(new { message = "Calibration profile not found" });

        // MVP: if UI can provide baseHours (raw expected) for datapoint, we use it; else conservative default 1.
        var baseHours = req.BaseHours ?? 1.0m;

        var updated = _calibration.UpdateFactor(profile.Factor, profile.Lambda, baseHours, req.ActualHours);
        profile.Factor = updated;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        _db.FeedbackEntries.Add(new FeedbackEntry
        {
            Id = Guid.NewGuid(),
            MappingVersionId = req.MappingVersionId,
            RuleActivityId = req.RuleActivityId,
            IntervalStartUtc = DateTime.SpecifyKind(req.IntervalStartUtc, DateTimeKind.Utc),
            ActualHours = req.ActualHours,
            Comment = req.Comment,
            CreatedByUserId = UserId(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return Ok(new { factor = updated });
    }

    private string UserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}
