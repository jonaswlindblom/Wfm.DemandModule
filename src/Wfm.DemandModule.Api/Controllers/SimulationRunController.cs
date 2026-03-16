using Microsoft.AspNetCore.Mvc;
using Wfm.DemandModule.Domain.Engine;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulation")]
public sealed class SimulationRunController : ControllerBase
{
    private static readonly CampingBookingCreatedWorkloadService Engine = new();

    [HttpGet("run")]
    public ActionResult<SimulationRunResponse> Run(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int intervalMinutes)
    {
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        if (toUtc <= fromUtc)
            return BadRequest(new { message = "to must be after from" });

        if (intervalMinutes <= 0 || 1440 % intervalMinutes != 0)
            return BadRequest(new { message = "intervalMinutes must be a positive divisor of 1440" });

        var buckets = new Dictionary<(string activityCode, DateTime intervalStartUtc), decimal>();

        foreach (var booking in BuildTestBookings(fromUtc, toUtc))
        {
            var bucketStartUtc = AlignToInterval(booking.OccurredAtUtc, intervalMinutes);

            foreach (var activity in Engine.Calculate(booking.Payload).Activities)
            {
                var key = (activity.ActivityCode, bucketStartUtc);
                buckets[key] = buckets.TryGetValue(key, out var current)
                    ? current + activity.Hours
                    : activity.Hours;
            }
        }

        var series = buckets
            .GroupBy(x => x.Key.activityCode)
            .Select(group => new ActivityTimeSeries(
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

        var allPoints = series.SelectMany(x => x.Points.Select(p => new { x.ActivityCode, Point = p })).ToArray();
        var groupedIntervals = buckets
            .GroupBy(x => x.Key.intervalStartUtc)
            .Select(group => new
            {
                IntervalStartUtc = group.Key,
                Hours = group.Sum(x => x.Value)
            })
            .OrderByDescending(x => x.Hours)
            .ThenBy(x => x.IntervalStartUtc)
            .FirstOrDefault();

        var summary = new RunSummary(
            TotalHours: totals.Sum(x => x.TotalHours),
            PeakIntervalStartUtc: groupedIntervals?.IntervalStartUtc,
            PeakIntervalHours: groupedIntervals?.Hours ?? 0m,
            ActivityCount: totals.Length,
            PrimaryDriver: "CampingBookingCreated");

        return Ok(new SimulationRunResponse(fromUtc, toUtc, intervalMinutes, series, totals, summary));
    }

    private static IEnumerable<TestBookingEvent> BuildTestBookings(DateTime fromUtc, DateTime toUtc)
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

    public sealed record SimulationRunResponse(
        DateTime From,
        DateTime To,
        int IntervalMinutes,
        IReadOnlyList<ActivityTimeSeries> Series,
        IReadOnlyList<ActivityTotal> Totals,
        RunSummary Summary);

    public sealed record ActivityTimeSeries(string ActivityCode, IReadOnlyList<TimeSeriesPoint> Points);
    public sealed record TimeSeriesPoint(DateTime IntervalStartUtc, decimal Hours);
    public sealed record ActivityTotal(string ActivityCode, decimal TotalHours);
    public sealed record RunSummary(decimal TotalHours, DateTime? PeakIntervalStartUtc, decimal PeakIntervalHours, int ActivityCount, string PrimaryDriver);
}
