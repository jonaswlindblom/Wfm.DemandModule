using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Wfm.DemandModule.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Swagger_IsReachable()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/swagger/index.html");
        Assert.True(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Can_Issue_Token()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new { userId = "jonas", role = "Admin" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.accessToken));
    }

    [Fact]
    public async Task Unauthorized_Stream_Create_IsDenied()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/streams", new { name = "X", sourceSystem = "POS", industry = "retail" });
        Assert.True((int)resp.StatusCode is 401 or 403);
    }

    [Fact]
    public async Task Simulation_Overview_Returns_Data_For_Dashboard()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/overview?date=2026-03-16");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SimulationOverviewResponse>();
        Assert.NotNull(body);
        Assert.Equal("2026-03-16", body!.date);
        Assert.Equal("day", body!.periods.day.key);
        Assert.NotEmpty(body.streams);
        Assert.NotEmpty(body.periods.day.chart);
        Assert.NotEmpty(body.periods.day.suggestions);
    }

    [Fact]
    public async Task Simulation_Overview_Week_Starts_On_Monday()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/overview?date=2026-03-16");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SimulationOverviewResponse>();
        Assert.NotNull(body);
        Assert.Equal(["mån", "tis", "ons", "tors", "fre", "lör", "sön"], body!.periods.week.chart.Select(x => x.label));
    }

    [Fact]
    public async Task Simulation_Run_Returns_TimeSeries_Totals_And_Summary()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/run?from=2026-03-16T00:00:00Z&to=2026-03-17T00:00:00Z&intervalMinutes=60");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SimulationRunResponse>();
        Assert.NotNull(body);
        Assert.Equal(60, body!.intervalMinutes);
        Assert.Equal("CampingBookingCreated", body.summary.primaryDriver);
        Assert.NotEmpty(body.series);
        Assert.NotEmpty(body.totals);
        Assert.True(body.summary.totalHours > 0);
    }

    [Fact]
    public async Task Simulation_Run_Returns_Reception_And_Housekeeping()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/run?from=2026-03-16T00:00:00Z&to=2026-03-17T00:00:00Z&intervalMinutes=60");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<SimulationRunResponse>();
        Assert.NotNull(body);

        Assert.Contains(body!.series, x => x.activityCode == "Reception");
        Assert.Contains(body.series, x => x.activityCode == "Housekeeping");
        Assert.Contains(body.totals, x => x.activityCode == "Reception" && x.totalHours > 0);
        Assert.Contains(body.totals, x => x.activityCode == "Housekeeping" && x.totalHours > 0);
    }

    [Fact]
    public async Task Simulation_Run_Invalid_Range_Returns_BadRequest()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/run?from=2026-03-17T00:00:00Z&to=2026-03-16T00:00:00Z&intervalMinutes=60");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Simulation_Run_Invalid_Interval_Returns_BadRequest()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/simulation/run?from=2026-03-16T00:00:00Z&to=2026-03-17T00:00:00Z&intervalMinutes=50");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private sealed record TokenResponse(string accessToken, DateTime expiresAtUtc);
    private sealed record SimulationOverviewResponse(string date, OverviewStream[] streams, OverviewPeriods periods);
    private sealed record OverviewPeriods(OverviewPeriod day, OverviewPeriod week);
    private sealed record OverviewPeriod(string key, OverviewChartPoint[] chart, OverviewSuggestion[] suggestions);
    private sealed record OverviewChartPoint(string label, decimal aiDemand);
    private sealed record OverviewSuggestion(int id, string title);
    private sealed record OverviewStream(string id, string name);
    private sealed record SimulationRunResponse(string from, string to, int intervalMinutes, ActivitySeries[] series, ActivityTotal[] totals, RunSummary summary);
    private sealed record ActivitySeries(string activityCode, TimeSeriesPoint[] points);
    private sealed record TimeSeriesPoint(string intervalStartUtc, decimal hours);
    private sealed record ActivityTotal(string activityCode, decimal totalHours);
    private sealed record RunSummary(decimal totalHours, string? peakIntervalStartUtc, decimal peakIntervalHours, int activityCount, string primaryDriver);
}
