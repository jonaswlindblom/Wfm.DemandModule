using Wfm.DemandModule.Domain.Engine;
using Xunit;

namespace Wfm.DemandModule.Tests;

public sealed class CampingBookingCreatedWorkloadServiceTests
{
    private readonly CampingBookingCreatedWorkloadService _service = new();

    [Fact]
    public void Calculate_ReturnsReceptionWorkload_BasePlusAddons()
    {
        var result = _service.Calculate(CreatePayload(addOns: ["LateCheckout", "Breakfast"]));

        var reception = result.Activities.Single(x => x.ActivityCode == "Reception");

        Assert.Equal(0.4m, reception.Hours);
        Assert.Equal(0.3m, reception.Explainability.BaseHours);
        Assert.Equal(2m, reception.Explainability.Units);
        Assert.Equal(0.05m, reception.Explainability.PerUnitHours);
    }

    [Fact]
    public void Calculate_ReturnsHousekeepingWorkload_ForStandardCabin()
    {
        var result = _service.Calculate(CreatePayload(
            checkIn: new DateOnly(2026, 3, 10),
            checkOut: new DateOnly(2026, 3, 13),
            cabinType: "Standard"));

        var housekeeping = result.Activities.Single(x => x.ActivityCode == "Housekeeping");

        Assert.Equal(1.8m, housekeeping.Hours);
        Assert.Equal(3m, housekeeping.Explainability.Units);
        Assert.Equal(1.0m, housekeeping.Explainability.Multiplier);
    }

    [Fact]
    public void Calculate_AppliesDeluxeCabinFactor_ToHousekeeping()
    {
        var result = _service.Calculate(CreatePayload(
            checkIn: new DateOnly(2026, 3, 10),
            checkOut: new DateOnly(2026, 3, 12),
            cabinType: "Deluxe"));

        var housekeeping = result.Activities.Single(x => x.ActivityCode == "Housekeeping");

        Assert.Equal(1.56m, housekeeping.Hours);
        Assert.Equal(1.3m, housekeeping.Explainability.Multiplier);
        Assert.Equal("cabinType=deluxe", housekeeping.Explainability.MultiplierReason);
    }

    [Fact]
    public void Calculate_AppliesTentCabinFactor_ToHousekeeping()
    {
        var result = _service.Calculate(CreatePayload(
            checkIn: new DateOnly(2026, 3, 10),
            checkOut: new DateOnly(2026, 3, 12),
            cabinType: "Tent"));

        var housekeeping = result.Activities.Single(x => x.ActivityCode == "Housekeeping");

        Assert.Equal(0.96m, housekeeping.Hours);
        Assert.Equal(0.8m, housekeeping.Explainability.Multiplier);
    }

    [Fact]
    public void Calculate_SameDayStay_ReturnsZeroHousekeepingButReceptionStillComputed()
    {
        var result = _service.Calculate(CreatePayload(
            checkIn: new DateOnly(2026, 3, 10),
            checkOut: new DateOnly(2026, 3, 10),
            addOns: []));

        var reception = result.Activities.Single(x => x.ActivityCode == "Reception");
        var housekeeping = result.Activities.Single(x => x.ActivityCode == "Housekeeping");

        Assert.Equal(0.3m, reception.Hours);
        Assert.Equal(0m, housekeeping.Hours);
        Assert.Equal(0m, housekeeping.Explainability.Units);
    }

    [Fact]
    public void Calculate_ReturnsExplainabilityPerActivity()
    {
        var result = _service.Calculate(CreatePayload());

        Assert.Equal(2, result.Activities.Count);
        Assert.All(result.Activities, activity =>
        {
            Assert.Equal(activity.Hours, activity.Explainability.ResultHours);
            Assert.False(string.IsNullOrWhiteSpace(activity.Explainability.Formula));
        });
    }

    private static CampingBookingCreatedPayload CreatePayload(
        DateOnly? checkIn = null,
        DateOnly? checkOut = null,
        string cabinType = "Standard",
        IReadOnlyList<string>? addOns = null)
        => new(
            BookingId: "booking-123",
            CheckInDate: checkIn ?? new DateOnly(2026, 3, 10),
            CheckOutDate: checkOut ?? new DateOnly(2026, 3, 12),
            Guests: 2,
            CabinType: cabinType,
            AddOns: addOns ?? ["Sauna"]
        );
}
