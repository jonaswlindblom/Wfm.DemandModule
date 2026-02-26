using System.Text.Json;
using Wfm.DemandModule.Domain.Engine;
using Wfm.DemandModule.Domain.Models;
using Xunit;

namespace Wfm.DemandModule.Tests;

public class EngineTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Reception_BasePlusAddons_ComputesCorrectly()
    {
        var streamEvent = new StreamEvent
        {
            Id = Guid.NewGuid(),
            EventType = "CampingBookingCreated",
            OccurredAtUtc = new DateTime(2026, 2, 26, 10, 07, 00, DateTimeKind.Utc),
            PayloadJson = JsonSerializer.Serialize(new { addOns = new[] { "A", "B" } })
        };

        var rule = new MappingRule { Id = Guid.NewGuid(), MappingVersionId = Guid.NewGuid(), EventType = "CampingBookingCreated", SortOrder = 1 };
        var actId = Guid.NewGuid();
        var ra = new MappingRuleActivity
        {
            Id = Guid.NewGuid(),
            MappingRuleId = rule.Id,
            ActivityId = actId,
            BaseHours = 0.3m,
            UnitExpression = "count($.addOns)",
            PerUnitHours = 0.05m
        };

        var (buckets, _) = _engine.Compute(
            new[] { streamEvent },
            new[] { rule },
            new[] { ra },
            new Dictionary<Guid, decimal> { [ra.Id] = 1.0m },
            streamEvent.OccurredAtUtc.AddHours(-1),
            streamEvent.OccurredAtUtc.AddHours(1),
            60);

        var key = (actId, new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));
        Assert.True(buckets.ContainsKey(key));
        Assert.Equal(0.3m + 2m * 0.05m, buckets[key]);
    }

    [Fact]
    public void Housekeeping_StayNights_ComputesCorrectly()
    {
        var ev = new StreamEvent
        {
            Id = Guid.NewGuid(),
            EventType = "CampingBookingCreated",
            OccurredAtUtc = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
            PayloadJson = "{\"checkInDate\":\"2026-02-20\",\"checkOutDate\":\"2026-02-23\",\"cabinType\":\"Standard\"}"
        };

        var rule = new MappingRule { Id = Guid.NewGuid(), MappingVersionId = Guid.NewGuid(), EventType = "CampingBookingCreated", SortOrder = 1 };
        var actId = Guid.NewGuid();
        var ra = new MappingRuleActivity
        {
            Id = Guid.NewGuid(),
            MappingRuleId = rule.Id,
            ActivityId = actId,
            BaseHours = 0m,
            UnitExpression = "stayNights($.checkInDate,$.checkOutDate)",
            PerUnitHours = 0.6m,
            MultiplierExpression = "cabinTypeFactor($.cabinType)"
        };

        var (buckets, _) = _engine.Compute(
            new[] { ev },
            new[] { rule },
            new[] { ra },
            new Dictionary<Guid, decimal>(),
            ev.OccurredAtUtc.AddHours(-1),
            ev.OccurredAtUtc.AddHours(1),
            60);

        var key = (actId, new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(3m * 0.6m * 1.0m, buckets[key]);
    }

    [Fact]
    public void CabinTypeFactor_Deluxe_IsApplied()
    {
        var ev = new StreamEvent
        {
            Id = Guid.NewGuid(),
            EventType = "CampingBookingCreated",
            OccurredAtUtc = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
            PayloadJson = "{\"checkInDate\":\"2026-02-20\",\"checkOutDate\":\"2026-02-21\",\"cabinType\":\"Deluxe\"}"
        };

        var rule = new MappingRule { Id = Guid.NewGuid(), MappingVersionId = Guid.NewGuid(), EventType = "CampingBookingCreated", SortOrder = 1 };
        var actId = Guid.NewGuid();
        var ra = new MappingRuleActivity
        {
            Id = Guid.NewGuid(),
            MappingRuleId = rule.Id,
            ActivityId = actId,
            BaseHours = 0m,
            UnitExpression = "stayNights($.checkInDate,$.checkOutDate)",
            PerUnitHours = 1.0m,
            MultiplierExpression = "cabinTypeFactor($.cabinType)"
        };

        var (buckets, _) = _engine.Compute(new[] { ev }, new[] { rule }, new[] { ra }, new Dictionary<Guid, decimal>(),
            ev.OccurredAtUtc.AddHours(-1), ev.OccurredAtUtc.AddHours(1), 60);

        var key = (actId, new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1m * 1.3m, buckets[key]);
    }

    [Fact]
    public void Condition_Equals_FiltersOut()
    {
        var ev = new StreamEvent
        {
            Id = Guid.NewGuid(),
            EventType = "CampingBookingCreated",
            OccurredAtUtc = new DateTime(2026, 2, 26, 12, 0, 0, DateTimeKind.Utc),
            PayloadJson = "{\"cabinType\":\"Standard\"}"
        };

        var rule = new MappingRule
        {
            Id = Guid.NewGuid(),
            MappingVersionId = Guid.NewGuid(),
            EventType = "CampingBookingCreated",
            SortOrder = 1,
            ConditionExpression = "equals($.cabinType,'Deluxe')"
        };

        var actId = Guid.NewGuid();
        var ra = new MappingRuleActivity { Id = Guid.NewGuid(), MappingRuleId = rule.Id, ActivityId = actId, BaseHours = 1m, PerUnitHours = 0m };

        var (buckets, _) = _engine.Compute(new[] { ev }, new[] { rule }, new[] { ra }, new Dictionary<Guid, decimal>(),
            ev.OccurredAtUtc.AddHours(-1), ev.OccurredAtUtc.AddHours(1), 60);

        Assert.Empty(buckets);
    }

    [Fact]
    public void CalibrationFactor_MultipliesResult()
    {
        var ev = new StreamEvent
        {
            Id = Guid.NewGuid(),
            EventType = "X",
            OccurredAtUtc = new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc),
            PayloadJson = "{}"
        };

        var rule = new MappingRule { Id = Guid.NewGuid(), MappingVersionId = Guid.NewGuid(), EventType = "X", SortOrder = 1 };
        var actId = Guid.NewGuid();
        var ra = new MappingRuleActivity { Id = Guid.NewGuid(), MappingRuleId = rule.Id, ActivityId = actId, BaseHours = 1m, PerUnitHours = 0m };

        var (buckets, _) = _engine.Compute(new[] { ev }, new[] { rule }, new[] { ra },
            new Dictionary<Guid, decimal> { [ra.Id] = 2.0m }, ev.OccurredAtUtc.AddHours(-1), ev.OccurredAtUtc.AddHours(1), 60);

        var key = (actId, new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(2m, buckets[key]);
    }
}
