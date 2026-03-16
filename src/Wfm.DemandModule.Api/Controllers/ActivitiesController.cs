using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;
using Wfm.DemandModule.Infrastructure.Services;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/activities")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly DemandDbContext _db;
    private readonly IAuditWriter _audit;

    public ActivitiesController(DemandDbContext db, IAuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<List<WorkActivity>>> GetAll(CancellationToken ct)
    {
        var items = await _db.WorkActivities
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        return Ok(items);
    }

    public sealed record CreateActivityRequest(string Code, string Name);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<WorkActivity>> Create([FromBody] CreateActivityRequest req, CancellationToken ct)
    {
        var a = new WorkActivity
        {
            Id = Guid.NewGuid(),
            Code = req.Code,
            Name = req.Name,
            IsActive = true
        };

        _db.WorkActivities.Add(a);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            actorUserId: UserId(),
            actorRole: Role(),
            action: "ActivityCreated",
            entityType: "WorkActivity",
            entityId: a.Id.ToString(),
            details: new { a.Code, a.Name, a.IsActive },
            ct: ct
        );

        return Ok(a);
    }

    public sealed record UpdateActivityRequest(string Name, bool IsActive);

    [HttpPut("{activityId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<WorkActivity>> Update(Guid activityId, [FromBody] UpdateActivityRequest req, CancellationToken ct)
    {
        var a = await _db.WorkActivities.FirstOrDefaultAsync(x => x.Id == activityId, ct);
        if (a is null)
            return NotFound(new { message = "Activity not found" });

        var before = new { a.Code, a.Name, a.IsActive };

        a.Name = req.Name;
        a.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            actorUserId: UserId(),
            actorRole: Role(),
            action: "ActivityUpdated",
            entityType: "WorkActivity",
            entityId: a.Id.ToString(),
            details: new { before, after = new { a.Code, a.Name, a.IsActive } },
            ct: ct
        );

        return Ok(a);
    }

    private string UserId() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    private string Role() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown";
}