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
