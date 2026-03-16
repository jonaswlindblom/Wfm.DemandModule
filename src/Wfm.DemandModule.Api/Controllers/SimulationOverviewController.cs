using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Engine;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulation")]
public sealed class SimulationOverviewController : ControllerBase
{
    private readonly DemandDbContext _db;
    private static readonly CampingSimulationService SimulationService = new(new CampingBookingCreatedWorkloadService());

    public SimulationOverviewController(DemandDbContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<SimulationOverviewResponse>> GetOverview([FromQuery] DateOnly? date = null, CancellationToken ct = default)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var streams = await BuildStreamsAsync(ct);

        return Ok(new SimulationOverviewResponse(
            DateTime.UtcNow,
            selectedDate,
            streams,
            new OverviewPeriods(
                BuildDayPeriod(selectedDate),
                BuildWeekPeriod(selectedDate),
                BuildMonthPeriod(selectedDate))));
    }

    private async Task<IReadOnlyList<OverviewStream>> BuildStreamsAsync(CancellationToken ct)
    {
        try
        {
            var dbStreams = await _db.DataStreams.AsNoTracking()
                .OrderBy(x => x.Name)
                .Take(6)
                .Select(x => new OverviewStream(
                    x.Id.ToString(),
                    x.Name,
                    x.SourceSystem,
                    "live",
                    x.Industry))
                .ToListAsync(ct);

            if (dbStreams.Count > 0)
            {
                return dbStreams;
            }
        }
        catch
        {
            // Keep overview reachable even when SQL is not configured for the current environment.
        }

        return SimulationService.BuildStreams()
            .Select(x => new OverviewStream(x.Id, x.Name, x.Type, x.Status, x.Value))
            .ToArray();
    }

    private static OverviewPeriod BuildDayPeriod(DateOnly selectedDate)
        => BuildPeriod(
            "day",
            selectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            selectedDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            60,
            selectedDate.ToString("yyyy-MM-dd"));

    private static OverviewPeriod BuildWeekPeriod(DateOnly selectedDate)
        => BuildPeriod(
            "week",
            selectedDate.AddDays(-3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            selectedDate.AddDays(4).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            1440,
            $"{selectedDate.AddDays(-3):yyyy-MM-dd} till {selectedDate.AddDays(3):yyyy-MM-dd}");

    private static OverviewPeriod BuildMonthPeriod(DateOnly selectedDate)
    {
        var monthStart = new DateTime(selectedDate.Year, selectedDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var daily = SimulationService.Run(monthStart, monthEnd, 1440);

        var weeks = daily.Series
            .SelectMany(s => s.Points.Select(p => new { s.ActivityCode, p.IntervalStartUtc, p.Hours }))
            .GroupBy(x => ISOWeek.GetWeekOfYear(x.IntervalStartUtc))
            .OrderBy(x => x.Key)
            .ToArray();

        var chart = weeks.Select(week =>
        {
            var total = week.Sum(x => x.Hours);
            return new OverviewChartPoint($"V.{week.Key}", total, total, null, (int)Math.Round(total * 11m), Math.Round(total * 0.9m, 1), Variance(total));
        }).ToArray();

        var mix = weeks.Select(week =>
        {
            var reception = week.Where(x => x.ActivityCode == "Reception").Sum(x => x.Hours);
            var housekeeping = week.Where(x => x.ActivityCode == "Housekeeping").Sum(x => x.Hours);
            return new OverviewActivityPoint($"V.{week.Key}", DecimalToInt(reception), 0, 0, DecimalToInt(housekeeping), 0);
        }).ToArray();

        var peak = chart.OrderByDescending(x => x.AiDemand).FirstOrDefault();
        var summary = new OverviewSummary(chart.Sum(x => x.AiDemand), 0m, peak?.Label ?? "-", peak?.AiDemand ?? 0m, 2, "CampingBookingCreated");

        return new OverviewPeriod("month", selectedDate.ToString("yyyy-MM"), summary, chart, mix, BuildMonthSuggestions(summary), BuildStrip(summary));
    }

    private static OverviewPeriod BuildPeriod(string key, DateTime fromUtc, DateTime toUtc, int intervalMinutes, string rangeLabel)
    {
        var result = SimulationService.Run(fromUtc, toUtc, intervalMinutes);
        var chart = BuildChart(result, key);
        var mix = BuildActivityMix(result, key);
        var peak = chart.OrderByDescending(x => x.AiDemand).FirstOrDefault();
        var summary = new OverviewSummary(result.Summary.TotalHours, 0m, peak?.Label ?? "-", result.Summary.PeakIntervalHours, 2, result.Summary.PrimaryDriver);

        return new OverviewPeriod(key, rangeLabel, summary, chart, mix, BuildSuggestions(result), BuildStrip(summary));
    }

    private static OverviewChartPoint[] BuildChart(SimulationResult result, string mode)
        => result.Series
            .SelectMany(s => s.Points.Select(p => new { s.ActivityCode, p.IntervalStartUtc, p.Hours }))
            .GroupBy(x => x.IntervalStartUtc)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var total = group.Sum(x => x.Hours);
                return new OverviewChartPoint(
                    CampingSimulationService.FormatLabel(group.Key, mode),
                    total,
                    total,
                    null,
                    (int)Math.Round(total * 12m),
                    Math.Round(total * 0.85m, 1),
                    Variance(total));
            })
            .ToArray();

    private static OverviewActivityPoint[] BuildActivityMix(SimulationResult result, string mode)
        => result.Series
            .SelectMany(s => s.Points.Select(p => new { s.ActivityCode, p.IntervalStartUtc, p.Hours }))
            .GroupBy(x => x.IntervalStartUtc)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var reception = group.Where(x => x.ActivityCode == "Reception").Sum(x => x.Hours);
                var housekeeping = group.Where(x => x.ActivityCode == "Housekeeping").Sum(x => x.Hours);
                return new OverviewActivityPoint(CampingSimulationService.FormatLabel(group.Key, mode), DecimalToInt(reception), 0, 0, DecimalToInt(housekeeping), 0);
            })
            .ToArray();

    private static OverviewSuggestion[] BuildSuggestions(SimulationResult result)
        => new[]
        {
            new OverviewSuggestion(1, "Öka Reception runt peak", "Fler bokningar med add-ons driver receptionstimmar i peak-fönstret.", $"+{Math.Round(result.Summary.PeakIntervalHours * 0.1m, 1)}h", 92, "increase"),
            new OverviewSuggestion(2, "Skydda Housekeeping-kapacitet", "Längre vistelser ökar städbehovet per bokning.", $"+{Math.Round(result.Totals.FirstOrDefault(x => x.ActivityCode == "Housekeeping")?.TotalHours * 0.08m ?? 0m, 1)}h", 88, "increase")
        };

    private static OverviewSuggestion[] BuildMonthSuggestions(OverviewSummary summary)
        => new[]
        {
            new OverviewSuggestion(6, "Säkra högsäsongsvecka", "Veckan med högst workload behöver extra kapacitetsbuffert.", $"+{Math.Round(summary.PeakHours * 0.05m, 1)}h", 89, "increase"),
            new OverviewSuggestion(7, "Optimera schemaläggning", "Jämnare veckofördelning kan minska buffertbehovet.", "-4.0h", 82, "decrease")
        };

    private static OverviewStripItem[] BuildStrip(OverviewSummary summary)
        => new[]
        {
            new OverviewStripItem("Bemanningstopp", summary.PeakLabel, $"{Math.Round(summary.PeakHours, 1)}h", "clock", "blue"),
            new OverviewStripItem("Högst Varians", summary.PeakLabel, "0%", "trending-up", "amber"),
            new OverviewStripItem("AI Konfidens", "92%", "Hög", "sparkles", "purple"),
            new OverviewStripItem("Risk", "Låg", "Stabil", "shield-alert", "emerald"),
            new OverviewStripItem("Buffert", "+5%", "God", "check-circle", "slate")
        };

    private static string Variance(decimal totalHours)
        => totalHours >= 5m ? "high" : totalHours >= 2m ? "medium" : "low";

    private static int DecimalToInt(decimal value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    public sealed record SimulationOverviewResponse(DateTime GeneratedAtUtc, DateOnly Date, IReadOnlyList<OverviewStream> Streams, OverviewPeriods Periods);
    public sealed record OverviewPeriods(OverviewPeriod Day, OverviewPeriod Week, OverviewPeriod Month);
    public sealed record OverviewPeriod(string Key, string RangeLabel, OverviewSummary Summary, IReadOnlyList<OverviewChartPoint> Chart, IReadOnlyList<OverviewActivityPoint> ActivityMix, IReadOnlyList<OverviewSuggestion> Suggestions, IReadOnlyList<OverviewStripItem> Strip);
    public sealed record OverviewSummary(decimal TotalHours, decimal DeltaPercent, string PeakLabel, decimal PeakHours, int ActiveAdjustments, string PrimaryDriver);
    public sealed record OverviewChartPoint(string Label, decimal Baseline, decimal AiDemand, decimal? Actual, int Visitors, decimal Sales, string Variance);
    public sealed record OverviewActivityPoint(string Label, int Foh, int Ops, int Fb, int Cleaning, int Security);
    public sealed record OverviewSuggestion(int Id, string Title, string Reason, string Impact, int Confidence, string Type);
    public sealed record OverviewStripItem(string Label, string Value, string Sub, string Icon, string Color);
    public sealed record OverviewStream(string Id, string Name, string Type, string Status, string Value);
}
