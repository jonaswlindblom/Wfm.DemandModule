using Microsoft.AspNetCore.Mvc;
using Wfm.DemandModule.Domain.Engine;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/simulation")]
public sealed class SimulationRunController : ControllerBase
{
    private static readonly CampingSimulationService SimulationService = new(new CampingBookingCreatedWorkloadService());

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

        var result = SimulationService.Run(fromUtc, toUtc, intervalMinutes);

        return Ok(new SimulationRunResponse(
            result.FromUtc,
            result.ToUtc,
            result.IntervalMinutes,
            result.Series.Select(x => new ActivityTimeSeries(x.ActivityCode, x.Points.Select(p => new TimeSeriesPoint(p.IntervalStartUtc, p.Hours)).ToArray())).ToArray(),
            result.Totals.Select(x => new ActivityTotal(x.ActivityCode, x.TotalHours)).ToArray(),
            new RunSummary(
                result.Summary.TotalHours,
                result.Summary.PeakIntervalStartUtc,
                result.Summary.PeakIntervalHours,
                result.Summary.ActivityCount,
                result.Summary.PrimaryDriver)));
    }

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
