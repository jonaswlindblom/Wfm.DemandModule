using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;
using Wfm.DemandModule.Infrastructure.Services;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/streams")]
public sealed class StreamsController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly IAuditWriter _audit;

    public StreamsController(DemandDbContext db, IAuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    public sealed record CreateStreamRequest(string Name, string SourceSystem, string Industry);

    [HttpPost]
    [Authorize(Policy = "PlannerOrAdmin")]
    public async Task<ActionResult<DataStream>> Create([FromBody] CreateStreamRequest req, CancellationToken ct)
    {
        var s = new DataStream
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            SourceSystem = req.SourceSystem,
            Industry = req.Industry,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.DataStreams.Add(s);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(UserId(), Role(), "StreamCreated", "DataStream", s.Id.ToString(),
            new { s.Name, s.SourceSystem, s.Industry }, ct);

        return CreatedAtAction(nameof(GetById), new { streamId = s.Id, version = "1" }, s);
    }

    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<List<DataStream>>> GetAll(CancellationToken ct)
        => Ok(await _db.DataStreams.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));

    [HttpGet("{streamId:guid}")]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<DataStream>> GetById(Guid streamId, CancellationToken ct)
    {
        var s = await _db.DataStreams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == streamId, ct);
        return s is null ? NotFound() : Ok(s);
    }

    private string UserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    private string Role() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown";
}
