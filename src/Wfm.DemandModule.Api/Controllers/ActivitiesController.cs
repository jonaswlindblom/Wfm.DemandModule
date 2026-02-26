using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/activities")]
public sealed class ActivitiesController : ControllerBase
{
    private readonly DemandDbContext _db;
    public ActivitiesController(DemandDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "ReadOnly")]
    public async Task<ActionResult<List<WorkActivity>>> GetAll(CancellationToken ct)
        => Ok(await _db.WorkActivities.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct));

    public sealed record CreateActivityRequest(string Code, string Name);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<WorkActivity>> Create([FromBody] CreateActivityRequest req, CancellationToken ct)
    {
        var a = new WorkActivity { Id = Guid.NewGuid(), Code = req.Code, Name = req.Name, IsActive = true };
        _db.WorkActivities.Add(a);
        await _db.SaveChangesAsync(ct);
        return Ok(a);
    }
}
