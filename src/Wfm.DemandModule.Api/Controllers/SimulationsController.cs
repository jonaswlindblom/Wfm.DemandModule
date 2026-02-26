using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Engine;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulations")]
public sealed class SimulationsController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly RuleEngine _engine;

    public SimulationsController(DemandDbContext db, RuleEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    public sealed record CreateSimulationRequest(Guid StreamId, Guid MappingVersionId, DateTime FromUtc, DateTime ToUtc, int IntervalMinutes);

    [HttpPost]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> Create([FromBody] CreateSimulationRequest req, CancellationToken ct)
    {
        var mv = await _db.MappingVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.MappingVersionId && x.StreamId == req.StreamId, ct);

        if (mv is null) return NotFound(new { message = "Mapping version not found for stream" });

        var fromUtc = DateTime.SpecifyKind(req.FromUtc, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(req.ToUtc, DateTimeKind.Utc);
        if (toUtc <= fromUtc) return BadRequest(new { message = "toUtc must be after fromUtc" });

        var events = await _db.StreamEvents.AsNoTracking()
            .Where(e => e.StreamId == req.StreamId && e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc < toUtc)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        var rules = await _db.MappingRules.AsNoTracking().Where(r => r.MappingVersionId == req.MappingVersionId).ToListAsync(ct);
        var ruleIds = rules.Select(r => r.Id).ToList();
        var ras = await _db.MappingRuleActivities.AsNoTracking().Where(a => ruleIds.Contains(a.MappingRuleId)).ToListAsync(ct);

        var cal = await _db.CalibrationProfiles.AsNoTracking()
            .Where(c => c.MappingVersionId == req.MappingVersionId)
            .ToDictionaryAsync(c => c.RuleActivityId, c => c.Factor, ct);

        var (buckets, contributions) = _engine.Compute(events, rules, ras, cal, fromUtc, toUtc, req.IntervalMinutes);

        var sim = new SimulationRun
        {
            Id = Guid.NewGuid(),
            StreamId = req.StreamId,
            MappingVersionId = req.MappingVersionId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            IntervalMinutes = req.IntervalMinutes,
            CreatedByUserId = UserId(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.SimulationRuns.Add(sim);

        foreach (var kv in buckets)
        {
            var (activityId, bucketStart) = kv.Key;
            var bucketEnd = bucketStart.AddMinutes(req.IntervalMinutes);

            var expl = contributions
                .Where(c => c.ActivityId == activityId && c.BucketStartUtc == bucketStart)
                .ToList();

            _db.WorkloadBuckets.Add(new WorkloadBucket
            {
                Id = Guid.NewGuid(),
                SimulationRunId = sim.Id,
                ActivityId = activityId,
                IntervalStartUtc = bucketStart,
                IntervalEndUtc = bucketEnd,
                Hours = kv.Value,
                ExplanationJson = JsonSerializer.Serialize(new
                {
                    activityId,
                    bucketStartUtc = bucketStart,
                    items = expl.Select(e => new
                    {
                        e.EventId, e.EventType, e.RuleId, e.RuleActivityId,
                        e.BaseHours, e.Units, e.PerUnitHours, e.Multiplier, e.Factor, e.ResultHours
                    })
                })
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { simulationId = sim.Id });
    }

    [HttpGet("{simulationId:guid}")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<SimulationRun>> Get(Guid simulationId, CancellationToken ct)
    {
        var sim = await _db.SimulationRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == simulationId, ct);
        return sim is null ? NotFound() : Ok(sim);
    }

    [HttpGet("{simulationId:guid}/buckets")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<List<WorkloadBucket>>> GetBuckets(Guid simulationId, CancellationToken ct)
    {
        var rows = await _db.WorkloadBuckets.AsNoTracking()
            .Where(x => x.SimulationRunId == simulationId)
            .OrderBy(x => x.IntervalStartUtc)
            .ToListAsync(ct);

        return Ok(rows);
    }

    private string UserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
}
