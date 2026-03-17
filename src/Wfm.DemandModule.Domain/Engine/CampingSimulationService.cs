using System.Globalization;

namespace Wfm.DemandModule.Domain.Engine;

public sealed class CampingSimulationService
{
    private readonly CampingBookingCreatedWorkloadService _workloadService;

    public CampingSimulationService(CampingBookingCreatedWorkloadService workloadService)
    {
        _workloadService = workloadService;
    }

    public SimulationResult Run(DateTime fromUtc, DateTime toUtc, int intervalMinutes)
    {
        var buckets = new Dictionary<(string activityCode, DateTime intervalStartUtc), decimal>();

        foreach (var booking in BuildTestBookings(fromUtc, toUtc))
        {
            var bucketStartUtc = AlignToInterval(booking.OccurredAtUtc, intervalMinutes);

            foreach (var activity in _workloadService.Calculate(booking.Payload).Activities)
            {
                var key = (activity.ActivityCode, bucketStartUtc);
                buckets[key] = buckets.TryGetValue(key, out var current)
                    ? current + activity.Hours
                    : activity.Hours;
            }
        }

        var series = buckets
            .GroupBy(x => x.Key.activityCode)
            .Select(group => new ActivitySeries(
                group.Key,
                group.OrderBy(x => x.Key.intervalStartUtc)
                    .Select(x => new TimeSeriesPoint(x.Key.intervalStartUtc, x.Value))
                    .ToArray()))
            .OrderBy(x => x.ActivityCode)
            .ToArray();

        var totals = series
            .Select(x => new ActivityTotal(x.ActivityCode, x.Points.Sum(p => p.Hours)))
            .OrderByDescending(x => x.TotalHours)
            .ToArray();

        var peakBucket = buckets
            .GroupBy(x => x.Key.intervalStartUtc)
            .Select(group => new { IntervalStartUtc = group.Key, Hours = group.Sum(x => x.Value) })
            .OrderByDescending(x => x.Hours)
            .ThenBy(x => x.IntervalStartUtc)
            .FirstOrDefault();

        var summary = new SimulationSummary(
            totals.Sum(x => x.TotalHours),
            peakBucket?.IntervalStartUtc,
            peakBucket?.Hours ?? 0m,
            totals.Length,
            "CampingBookingCreated");

        return new SimulationResult(fromUtc, toUtc, intervalMinutes, series, totals, summary);
    }

    public IReadOnlyList<TestStreamInfo> BuildStreams() =>
        new[]
        {
            new TestStreamInfo("bookings", "Camping Bookings", "PMS", "live", "CampingBookingCreated"),
            new TestStreamInfo("activities", "Work Activities", "WFM", "live", "Reception + Housekeeping"),
            new TestStreamInfo("mapping", "Mapping Rules", "Rules Engine", "live", "Active camping mapping")
        };

    public static string FormatLabel(DateTime intervalStartUtc, string mode)
        => mode switch
        {
            "day" => intervalStartUtc.ToString("HH:mm", CultureInfo.InvariantCulture),
            "week" => intervalStartUtc.ToString("ddd", new CultureInfo("sv-SE")),
            "month" => $"V.{ISOWeek.GetWeekOfYear(intervalStartUtc)}",
            _ => intervalStartUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        };

    private IEnumerable<TestBookingEvent> BuildTestBookings(DateTime fromUtc, DateTime toUtc)
    {
        for (var day = fromUtc.Date; day < toUtc.Date.AddDays(1); day = day.AddDays(1))
        {
            foreach (var slot in BuildDailyBookings(day))
            {
                if (slot.OccurredAtUtc >= fromUtc && slot.OccurredAtUtc < toUtc)
                {
                    yield return slot;
                }
            }
        }
    }

    private static IReadOnlyList<TestBookingEvent> BuildDailyBookings(DateTime dayUtc)
    {
        var date = DateOnly.FromDateTime(dayUtc);
        var dayIndex = dayUtc.DayOfYear;

        return new[]
        {
            new TestBookingEvent(
                dayUtc.AddHours(9),
                new CampingBookingCreatedPayload(
                    $"booking-{date:yyyyMMdd}-morning",
                    date.AddDays(1),
                    date.AddDays(3),
                    2,
                    "Standard",
                    new[] { "Breakfast" })),
            new TestBookingEvent(
                dayUtc.AddHours(12),
                new CampingBookingCreatedPayload(
                    $"booking-{date:yyyyMMdd}-midday",
                    date.AddDays(2),
                    date.AddDays(6),
                    4,
                    dayIndex % 2 == 0 ? "Deluxe" : "Standard",
                    new[] { "Sauna", "LateCheckout" })),
            new TestBookingEvent(
                dayUtc.AddHours(17),
                new CampingBookingCreatedPayload(
                    $"booking-{date:yyyyMMdd}-evening",
                    date.AddDays(3),
                    date.AddDays(dayIndex % 3 == 0 ? 4 : 5),
                    3,
                    dayIndex % 5 == 0 ? "Tent" : "Standard",
                    Array.Empty<string>()))
        };
    }

    private static DateTime AlignToInterval(DateTime utc, int intervalMinutes)
    {
        var intervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks;
        var aligned = utc.Ticks - (utc.Ticks % intervalTicks);
        return new DateTime(aligned, DateTimeKind.Utc);
    }

    private sealed record TestBookingEvent(DateTime OccurredAtUtc, CampingBookingCreatedPayload Payload);
}

public sealed record SimulationResult(
    DateTime FromUtc,
    DateTime ToUtc,
    int IntervalMinutes,
    IReadOnlyList<ActivitySeries> Series,
    IReadOnlyList<ActivityTotal> Totals,
    SimulationSummary Summary);

public sealed record ActivitySeries(string ActivityCode, IReadOnlyList<TimeSeriesPoint> Points);
public sealed record TimeSeriesPoint(DateTime IntervalStartUtc, decimal Hours);
public sealed record ActivityTotal(string ActivityCode, decimal TotalHours);
public sealed record SimulationSummary(decimal TotalHours, DateTime? PeakIntervalStartUtc, decimal PeakIntervalHours, int ActivityCount, string PrimaryDriver);
public sealed record TestStreamInfo(string Id, string Name, string Type, string Status, string Value);
