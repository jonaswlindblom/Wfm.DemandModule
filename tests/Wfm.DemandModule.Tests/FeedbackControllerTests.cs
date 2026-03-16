using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Api.Controllers;
using Wfm.DemandModule.Domain.Engine;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;
using Xunit;

namespace Wfm.DemandModule.Tests;

public sealed class FeedbackControllerTests
{
    [Fact]
    public async Task Get_Returns_Recent_Entries_And_Profiles()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedFeedbackFixture(db);
        var controller = CreateController(db);

        var result = await controller.Get(null, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = ok.Value!.GetType().GetProperty("entries")!.GetValue(ok.Value) as System.Collections.IEnumerable;
        var profiles = ok.Value.GetType().GetProperty("profiles")!.GetValue(ok.Value) as System.Collections.IEnumerable;

        Assert.NotNull(entries);
        Assert.NotNull(profiles);
        Assert.Contains(entries!.Cast<object>(), x => GetProperty<Guid>(x, "MappingVersionId") == fixture.MappingVersionId);
        Assert.Contains(profiles!.Cast<object>(), x => GetProperty<decimal>(x, "Factor") > 1.0m);
    }

    [Fact]
    public async Task Get_Filters_By_MappingVersion()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedFeedbackFixture(db);
        var otherVersionId = Guid.NewGuid();
        db.MappingVersions.Add(new MappingVersion
        {
            Id = otherVersionId,
            StreamId = fixture.StreamId,
            VersionNumber = 2,
            Name = "v2",
            CreatedByUserId = "tester",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.Get(fixture.MappingVersionId, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = ((System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("entries")!.GetValue(ok.Value)!).Cast<object>().ToList();

        Assert.NotEmpty(entries);
        Assert.All(entries, x => Assert.Equal(fixture.MappingVersionId, GetProperty<Guid>(x, "MappingVersionId")));
    }

    [Fact]
    public async Task Create_Updates_Profile_And_Stores_Entry()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedFeedbackFixture(db, includeEntry: false);
        var controller = CreateController(db);

        var result = await controller.Create(new FeedbackController.CreateFeedbackRequest(
            fixture.MappingVersionId,
            fixture.RuleActivityId,
            new DateTime(2026, 3, 16, 10, 0, 0, DateTimeKind.Utc),
            2.4m,
            2.0m,
            "Higher than expected"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);

        var profile = await db.CalibrationProfiles.SingleAsync();
        var entry = await db.FeedbackEntries.SingleAsync();

        Assert.True(profile.Factor > 1.0m);
        Assert.Equal("Higher than expected", entry.Comment);
        Assert.Equal("tester", entry.CreatedByUserId);
    }

    private static DemandDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DemandDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DemandDbContext(options);
    }

    private static FeedbackController CreateController(DemandDbContext db)
    {
        var controller = new FeedbackController(db, new CalibrationService());
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

    private static async Task<(Guid StreamId, Guid MappingVersionId, Guid RuleActivityId)> SeedFeedbackFixture(DemandDbContext db, bool includeEntry = true)
    {
        var streamId = Guid.NewGuid();
        var mappingVersionId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var ruleActivityId = Guid.NewGuid();

        db.DataStreams.Add(new DataStream
        {
            Id = streamId,
            Name = "Camping",
            SourceSystem = "PMS",
            Industry = "Camping"
        });
        db.WorkActivities.Add(new WorkActivity
        {
            Id = activityId,
            Code = "HOUSEKEEPING",
            Name = "Housekeeping",
            IsActive = true
        });
        db.MappingVersions.Add(new MappingVersion
        {
            Id = mappingVersionId,
            StreamId = streamId,
            VersionNumber = 1,
            Name = "v1",
            CreatedByUserId = "tester",
            IsActive = true
        });
        db.MappingRules.Add(new MappingRule
        {
            Id = ruleId,
            MappingVersionId = mappingVersionId,
            EventType = "CampingBookingCreated",
            Name = "base",
            SortOrder = 1
        });
        db.MappingRuleActivities.Add(new MappingRuleActivity
        {
            Id = ruleActivityId,
            MappingRuleId = ruleId,
            ActivityId = activityId,
            BaseHours = 0.6m,
            UnitExpression = "stayNights($.checkInDate,$.checkOutDate)",
            PerUnitHours = 0.6m
        });
        db.CalibrationProfiles.Add(new CalibrationProfile
        {
            Id = Guid.NewGuid(),
            MappingVersionId = mappingVersionId,
            RuleActivityId = ruleActivityId,
            Factor = 1.15m,
            Lambda = 0.1m
        });

        if (includeEntry)
        {
            db.FeedbackEntries.Add(new FeedbackEntry
            {
                Id = Guid.NewGuid(),
                MappingVersionId = mappingVersionId,
                RuleActivityId = ruleActivityId,
                IntervalStartUtc = new DateTime(2026, 3, 16, 8, 0, 0, DateTimeKind.Utc),
                ActualHours = 2.1m,
                Comment = "Observed higher demand",
                CreatedByUserId = "tester",
                CreatedAtUtc = new DateTime(2026, 3, 16, 9, 0, 0, DateTimeKind.Utc)
            });
        }

        await db.SaveChangesAsync();
        return (streamId, mappingVersionId, ruleActivityId);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        return (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;
    }
}
