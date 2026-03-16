using Microsoft.AspNetCore.Mvc;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulation")]
public sealed class SimulationOverviewController : ControllerBase
{
    [HttpGet("overview")]
    public ActionResult<SimulationOverviewResponse> GetOverview([FromQuery] DateOnly? date = null)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return Ok(new SimulationOverviewResponse(
            GeneratedAtUtc: DateTime.UtcNow,
            Date: selectedDate,
            Streams: BuildStreams(),
            Periods: new OverviewPeriods(
                Day: BuildDayPeriod(selectedDate),
                Week: BuildWeekPeriod(selectedDate),
                Month: BuildMonthPeriod(selectedDate))));
    }

    private static IReadOnlyList<OverviewStream> BuildStreams() =>
        new[]
        {
            new OverviewStream("visitors", "Besöksräknare", "IoT Sensor", "live", "432/h"),
            new OverviewStream("sales", "Kassatransaktioner", "API", "live", "24 kkr/h"),
            new OverviewStream("bookings", "Bokningar", "Intern DB", "live", "45 aktiva"),
            new OverviewStream("weather", "Lokalt Väder", "Extern API", "live", "Regn 2mm"),
            new OverviewStream("campaigns", "Kampanjer", "CMS", "live", "2 aktiva")
        };

    private static OverviewPeriod BuildDayPeriod(DateOnly selectedDate) =>
        new(
            Key: "day",
            RangeLabel: selectedDate.ToString("yyyy-MM-dd"),
            Summary: new OverviewSummary(342.5m, 8.4m, "17:00", 42m, 3, "CampingBookingCreated"),
            Chart: new[]
            {
                new OverviewChartPoint("08:00", 12m, 12m, 11m, 120, 12m, "low"),
                new OverviewChartPoint("09:00", 15m, 16m, 16m, 200, 18m, "low"),
                new OverviewChartPoint("10:00", 22m, 24m, 23m, 350, 25m, "medium"),
                new OverviewChartPoint("11:00", 30m, 35m, 36m, 580, 40m, "high"),
                new OverviewChartPoint("12:00", 45m, 48m, null, 800, 55m, "medium"),
                new OverviewChartPoint("13:00", 42m, 46m, null, 750, 50m, "medium"),
                new OverviewChartPoint("14:00", 30m, 32m, null, 400, 30m, "low"),
                new OverviewChartPoint("15:00", 25m, 24m, null, 300, 22m, "low"),
                new OverviewChartPoint("16:00", 25m, 28m, null, 380, 28m, "medium"),
                new OverviewChartPoint("17:00", 35m, 42m, null, 600, 45m, "high"),
                new OverviewChartPoint("18:00", 40m, 42m, null, 650, 48m, "low"),
                new OverviewChartPoint("19:00", 30m, 30m, null, 400, 35m, "low")
            },
            ActivityMix: new[]
            {
                new OverviewActivityPoint("08:00", 4, 2, 3, 2, 1),
                new OverviewActivityPoint("09:00", 6, 3, 4, 2, 1),
                new OverviewActivityPoint("10:00", 9, 4, 6, 3, 2),
                new OverviewActivityPoint("11:00", 13, 7, 8, 5, 2),
                new OverviewActivityPoint("12:00", 18, 9, 12, 6, 3),
                new OverviewActivityPoint("13:00", 17, 9, 11, 6, 3),
                new OverviewActivityPoint("14:00", 12, 6, 8, 4, 2),
                new OverviewActivityPoint("15:00", 9, 5, 6, 3, 1),
                new OverviewActivityPoint("16:00", 11, 5, 7, 4, 1),
                new OverviewActivityPoint("17:00", 16, 8, 10, 5, 3),
                new OverviewActivityPoint("18:00", 17, 8, 10, 5, 2),
                new OverviewActivityPoint("19:00", 12, 6, 7, 4, 1)
            },
            Suggestions: new[]
            {
                new OverviewSuggestion(1, "Öka Reception 11:00-13:00", "CampingBookingCreated med många addons driver mer incheckningsarbete än regelbasen.", "+4.5h", 94, "increase"),
                new OverviewSuggestion(2, "Säkra Housekeeping-fönster 12:00", "Längre stay-nights i Deluxe-kabiner ökar städbehovet per bokning.", "+3.0h", 91, "increase"),
                new OverviewSuggestion(3, "Behåll kvällsbuffert låg", "Bokningsinflödet faller efter 18:00 och inga extra addons väntas.", "-2.0h", 86, "decrease")
            },
            Strip: new[]
            {
                new OverviewStripItem("Bemanningstopp", "17:00", "42h", "clock", "blue"),
                new OverviewStripItem("Högst Varians", "11:00", "+5h", "trending-up", "amber"),
                new OverviewStripItem("AI Konfidens", "94%", "Hög", "sparkles", "purple"),
                new OverviewStripItem("Risk", "Låg", "<2%", "shield-alert", "emerald"),
                new OverviewStripItem("Buffert", "+5%", "Optimal", "check-circle", "slate")
            });

    private static OverviewPeriod BuildWeekPeriod(DateOnly selectedDate) =>
        new(
            Key: "week",
            RangeLabel: $"{selectedDate.AddDays(-3):yyyy-MM-dd} till {selectedDate.AddDays(3):yyyy-MM-dd}",
            Summary: new OverviewSummary(2800m, 4.2m, "Fredag", 480m, 2, "CampingBookingCreated"),
            Chart: new[]
            {
                new OverviewChartPoint("Mån", 320m, 330m, 325m, 4200, 320m, "low"),
                new OverviewChartPoint("Tis", 310m, 315m, 318m, 4100, 310m, "low"),
                new OverviewChartPoint("Ons", 340m, 360m, null, 4800, 350m, "medium"),
                new OverviewChartPoint("Tors", 380m, 410m, null, 5200, 400m, "high"),
                new OverviewChartPoint("Fre", 450m, 480m, null, 6500, 550m, "high"),
                new OverviewChartPoint("Lör", 520m, 510m, null, 7200, 600m, "low"),
                new OverviewChartPoint("Sön", 400m, 395m, null, 5500, 420m, "low")
            },
            ActivityMix: new[]
            {
                new OverviewActivityPoint("Mån", 132, 66, 82, 33, 17),
                new OverviewActivityPoint("Tis", 126, 63, 79, 31, 16),
                new OverviewActivityPoint("Ons", 144, 72, 90, 36, 18),
                new OverviewActivityPoint("Tors", 164, 82, 102, 41, 21),
                new OverviewActivityPoint("Fre", 192, 96, 120, 48, 24),
                new OverviewActivityPoint("Lör", 204, 102, 127, 51, 26),
                new OverviewActivityPoint("Sön", 158, 79, 98, 39, 20)
            },
            Suggestions: new[]
            {
                new OverviewSuggestion(4, "Helgbemanning Lördag", "Hög bokningstakt och längre vistelser ökar housekeeping-behovet.", "+24h", 91, "increase"),
                new OverviewSuggestion(5, "Minska Tisdag Morgon", "Låg trend för nya bokningar i början av veckan.", "-8.0h", 85, "decrease")
            },
            Strip: new[]
            {
                new OverviewStripItem("Bemanningstopp", "Fredag", "480h", "clock", "blue"),
                new OverviewStripItem("Högst Varians", "Fredag", "+12%", "trending-up", "amber"),
                new OverviewStripItem("AI Konfidens", "89%", "Medel", "sparkles", "purple"),
                new OverviewStripItem("Risk", "Medel", "Fre em", "shield-alert", "amber"),
                new OverviewStripItem("Buffert", "+2%", "Tight", "check-circle", "slate")
            });

    private static OverviewPeriod BuildMonthPeriod(DateOnly selectedDate) =>
        new(
            Key: "month",
            RangeLabel: selectedDate.ToString("yyyy-MM"),
            Summary: new OverviewSummary(10400m, 2.1m, "V.44", 2900m, 2, "CampingBookingCreated"),
            Chart: new[]
            {
                new OverviewChartPoint("V.41", 2400m, 2450m, 2440m, 32000, 2400m, "low"),
                new OverviewChartPoint("V.42", 2550m, 2600m, 2580m, 34000, 2550m, "low"),
                new OverviewChartPoint("V.43", 2300m, 2450m, null, 31000, 2300m, "high"),
                new OverviewChartPoint("V.44", 2800m, 2900m, null, 38000, 2900m, "medium")
            },
            ActivityMix: new[]
            {
                new OverviewActivityPoint("V.41", 980, 490, 612, 245, 123),
                new OverviewActivityPoint("V.42", 1040, 520, 650, 260, 130),
                new OverviewActivityPoint("V.43", 980, 490, 612, 245, 123),
                new OverviewActivityPoint("V.44", 1160, 580, 725, 290, 145)
            },
            Suggestions: new[]
            {
                new OverviewSuggestion(6, "Höstlovsjustering V.44", "Skollov och fler bokningar ökar reception och städ samtidigt.", "+120h", 89, "increase"),
                new OverviewSuggestion(7, "Optimera Schemaläggning", "Jämnare bokningskurva i V.42 ger lägre buffertbehov.", "-40h", 82, "decrease")
            },
            Strip: new[]
            {
                new OverviewStripItem("Bemanningstopp", "V.44", "2,900h", "clock", "blue"),
                new OverviewStripItem("Högst Varians", "V.43", "-5%", "trending-up", "amber"),
                new OverviewStripItem("AI Konfidens", "92%", "Hög", "sparkles", "purple"),
                new OverviewStripItem("Risk", "Låg", "Stabil", "shield-alert", "emerald"),
                new OverviewStripItem("Buffert", "+8%", "God", "check-circle", "slate")
            });

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
