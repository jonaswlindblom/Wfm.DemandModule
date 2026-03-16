using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Api.Controllers;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;
using Wfm.DemandModule.Infrastructure.Services;
using Xunit;

namespace Wfm.DemandModule.Tests;

public sealed class MappingsControllerTests
{
    [Fact]
    public async Task GetActive_Returns_Current_Active_Version()
    {
        await using var db = CreateDbContext();
        var streamId = Guid.NewGuid();
        db.DataStreams.Add(new DataStream { Id = streamId, Name = "Camping", SourceSystem = "PMS", Industry = "Camping" });
        db.MappingVersions.Add(new MappingVersion
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            VersionNumber = 1,
            Name = "v1",
            CreatedByUserId = "tester",
            IsActive = true
        });
        db.MappingVersions.Add(new MappingVersion
        {
            Id = Guid.NewGuid(),
            StreamId = streamId,
            VersionNumber = 2,
            Name = "v2",
            CreatedByUserId = "tester",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetActive(streamId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var version = ok.Value!.GetType().GetProperty("version")!.GetValue(ok.Value) as MappingVersion;
        Assert.NotNull(version);
        Assert.Equal(1, version!.VersionNumber);
        Assert.True(version.IsActive);
    }

    [Fact]
    public async Task Activate_Marks_Only_Target_Version_As_Active()
    {
        await using var db = CreateDbContext();
        var streamId = Guid.NewGuid();
        var first = new MappingVersion { Id = Guid.NewGuid(), StreamId = streamId, VersionNumber = 1, Name = "v1", CreatedByUserId = "tester", IsActive = true };
        var second = new MappingVersion { Id = Guid.NewGuid(), StreamId = streamId, VersionNumber = 2, Name = "v2", CreatedByUserId = "tester", IsActive = false };
        db.DataStreams.Add(new DataStream { Id = streamId, Name = "Camping", SourceSystem = "PMS", Industry = "Camping" });
        db.MappingVersions.AddRange(first, second);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Activate(streamId, second.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var versions = await db.MappingVersions.Where(x => x.StreamId == streamId).OrderBy(x => x.VersionNumber).ToListAsync();
        Assert.False(versions[0].IsActive);
        Assert.True(versions[1].IsActive);
        Assert.Single(await db.AuditLogEntries.ToListAsync());
    }

    [Fact]
    public async Task Create_First_Version_Becomes_Active()
    {
        await using var db = CreateDbContext();
        var streamId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        db.DataStreams.Add(new DataStream { Id = streamId, Name = "Camping", SourceSystem = "PMS", Industry = "Camping" });
        db.WorkActivities.Add(new WorkActivity { Id = activityId, Code = "RECEPTION", Name = "Reception", IsActive = true });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var request = new MappingsController.CreateMappingRequest(
            "Initial version",
            new List<MappingsController.RuleDto>
            {
                new(
                    "CampingBookingCreated",
                    "CampingBookingCreated",
                    null,
                    1,
                    new List<MappingsController.RuleActivityDto>
                    {
                        new(activityId, 0.3m, "count($.addOns)", 0.05m, null)
                    })
            });

        var result = await controller.Create(streamId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var version = await db.MappingVersions.SingleAsync();
        Assert.True(version.IsActive);
        Assert.Equal(1, version.VersionNumber);
    }

    private static DemandDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DemandDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DemandDbContext(options);
    }

    private static MappingsController CreateController(DemandDbContext db)
    {
        var controller = new MappingsController(db, new AuditWriter(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "tester"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "test"))
            }
        };
        return controller;
    }
}
