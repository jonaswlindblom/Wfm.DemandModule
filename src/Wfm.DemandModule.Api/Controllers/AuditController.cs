using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly DemandDbContext _db;
    public AuditController(DemandDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> Get([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] string? actorUserId, CancellationToken ct)
    {
        var q = _db.AuditLogEntries.AsNoTracking().AsQueryable();

        if (fromUtc.HasValue) q = q.Where(x => x.OccurredAtUtc >= fromUtc.Value.ToUniversalTime());
        if (toUtc.HasValue) q = q.Where(x => x.OccurredAtUtc < toUtc.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(actorUserId)) q = q.Where(x => x.ActorUserId == actorUserId);

        var rows = await q.OrderByDescending(x => x.OccurredAtUtc).Take(500).ToListAsync(ct);
        return Ok(rows);
    }
}
