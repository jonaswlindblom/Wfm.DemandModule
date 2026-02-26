namespace Wfm.DemandModule.Domain.Engine;

public sealed class CalibrationService
{
    public decimal UpdateFactor(decimal currentFactor, decimal lambda, decimal baseHours, decimal actualHours)
    {
        if (baseHours <= 0m) return currentFactor;

        var target = actualHours / baseHours;
        var alpha = 0.2m;

        var shrunk = target / (1m + lambda);
        var next = currentFactor * (1m - alpha) + shrunk * alpha;

        return Clamp(next, 0m, 10m);
    }

    private static decimal Clamp(decimal v, decimal min, decimal max)
        => v < min ? min : (v > max ? max : v);
}
