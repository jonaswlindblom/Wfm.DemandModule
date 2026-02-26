using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/streams/{streamId:guid}/events")]
public sealed class EventsController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly ILogger<EventsController> _log;

    public EventsController(DemandDbContext db, ILogger<EventsController> log)
    {
        _db = db;
        _log = log;
    }

    public sealed record IngestEventDto(string EventKey, string EventType, DateTime OccurredAtUtc, string PayloadJson);

    [HttpPost("batch")]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<object>> IngestBatch(Guid streamId, [FromBody] List<IngestEventDto> batch, CancellationToken ct)
    {
        if (!await _db.DataStreams.AnyAsync(x => x.Id == streamId, ct))
            return NotFound(new { message = "Stream not found" });

        var inserted = 0;
        var skipped = 0;

        foreach (var dto in batch)
        {
            var exists = await _db.StreamEvents.AnyAsync(x => x.EventKey == dto.EventKey, ct);
            if (exists) { skipped++; continue; }

            _db.StreamEvents.Add(new StreamEvent
            {
                Id = Guid.NewGuid(),
                StreamId = streamId,
                EventKey = dto.EventKey,
                EventType = dto.EventType,
                OccurredAtUtc = DateTime.SpecifyKind(dto.OccurredAtUtc, DateTimeKind.Utc),
                PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson,
                IngestedAtUtc = DateTime.UtcNow
            });

            inserted++;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _log.LogWarning(ex, "DbUpdateException during batch ingest; treating as idempotent conflict.");
        }

        return Ok(new { inserted, skipped });
    }
}
