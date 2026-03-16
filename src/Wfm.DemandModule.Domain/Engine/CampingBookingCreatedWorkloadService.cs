namespace Wfm.DemandModule.Domain.Engine;

public sealed record CampingBookingCreatedPayload(
    string BookingId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Guests,
    string CabinType,
    IReadOnlyList<string> AddOns
);

public sealed record ActivityExplainability(
    string ActivityCode,
    decimal BaseHours,
    decimal Units,
    string UnitLabel,
    decimal PerUnitHours,
    decimal Multiplier,
    string MultiplierReason,
    decimal ResultHours,
    string Formula
);

public sealed record ActivityWorkload(
    string ActivityCode,
    decimal Hours,
    ActivityExplainability Explainability
);

public sealed record CampingBookingCreatedWorkloadResult(
    string BookingId,
    IReadOnlyList<ActivityWorkload> Activities
);

public sealed class CampingBookingCreatedWorkloadService
{
    private const decimal ReceptionBaseHours = 0.3m;
    private const decimal ReceptionPerAddonHours = 0.05m;
    private const decimal HousekeepingPerNightHours = 0.6m;

    public CampingBookingCreatedWorkloadResult Calculate(CampingBookingCreatedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var addOnCount = payload.AddOns?.Count ?? 0;
        var stayNights = Math.Max(0, payload.CheckOutDate.DayNumber - payload.CheckInDate.DayNumber);
        var cabinTypeFactor = GetCabinTypeFactor(payload.CabinType);
        var multiplierReason = $"cabinType={NormalizeCabinType(payload.CabinType)}";

        var receptionHours = ReceptionBaseHours + addOnCount * ReceptionPerAddonHours;
        var housekeepingHours = stayNights * HousekeepingPerNightHours * cabinTypeFactor;

        var activities = new[]
        {
            new ActivityWorkload(
                "Reception",
                receptionHours,
                new ActivityExplainability(
                    "Reception",
                    ReceptionBaseHours,
                    addOnCount,
                    "addOns",
                    ReceptionPerAddonHours,
                    1.0m,
                    "fixed",
                    receptionHours,
                    "0.3h + 0.05h per addon"
                )
            ),
            new ActivityWorkload(
                "Housekeeping",
                housekeepingHours,
                new ActivityExplainability(
                    "Housekeeping",
                    0m,
                    stayNights,
                    "stayNights",
                    HousekeepingPerNightHours,
                    cabinTypeFactor,
                    multiplierReason,
                    housekeepingHours,
                    "0.6h per stay-night adjusted by cabinType"
                )
            )
        };

        return new CampingBookingCreatedWorkloadResult(payload.BookingId, activities);
    }

    private static decimal GetCabinTypeFactor(string? cabinType)
        => NormalizeCabinType(cabinType) switch
        {
            "deluxe" => 1.3m,
            "tent" => 0.8m,
            "standard" => 1.0m,
            _ => 1.0m
        };

    private static string NormalizeCabinType(string? cabinType)
        => string.IsNullOrWhiteSpace(cabinType) ? "standard" : cabinType.Trim().ToLowerInvariant();
}
