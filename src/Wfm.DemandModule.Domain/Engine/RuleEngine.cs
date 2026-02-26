using System.Text.Json;
using System.Text.RegularExpressions;
using Wfm.DemandModule.Domain.Models;

namespace Wfm.DemandModule.Domain.Engine;

public sealed record Contribution(
    Guid EventId,
    string EventType,
    Guid RuleId,
    Guid RuleActivityId,
    Guid ActivityId,
    DateTime BucketStartUtc,
    decimal BaseHours,
    decimal Units,
    decimal PerUnitHours,
    decimal Multiplier,
    decimal Factor,
    decimal ResultHours
);

public sealed class RuleEngine
{
    public (Dictionary<(Guid activityId, DateTime bucketStartUtc), decimal> buckets, List<Contribution> contributions)
        Compute(
            IReadOnlyList<StreamEvent> events,
            IReadOnlyList<MappingRule> rules,
            IReadOnlyList<MappingRuleActivity> ruleActivities,
            IReadOnlyDictionary<Guid, decimal> calibrationFactors,
            DateTime fromUtc,
            DateTime toUtc,
            int intervalMinutes)
    {
        var buckets = new Dictionary<(Guid, DateTime), decimal>();
        var contributions = new List<Contribution>();

        var activitiesByRule = ruleActivities
            .GroupBy(a => a.MappingRuleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ev in events)
        {
            if (ev.OccurredAtUtc < fromUtc || ev.OccurredAtUtc >= toUtc) continue;

            var bucketStart = AlignToInterval(ev.OccurredAtUtc, intervalMinutes);

            foreach (var rule in rules.Where(r => r.EventType == ev.EventType).OrderBy(r => r.SortOrder))
            {
                if (!EvaluateCondition(rule.ConditionExpression, ev.PayloadJson))
                    continue;

                if (!activitiesByRule.TryGetValue(rule.Id, out var ras)) continue;

                foreach (var ra in ras)
                {
                    var baseHours = ra.BaseHours;
                    var units = EvaluateUnits(ra.UnitExpression, ev.PayloadJson);
                    var mult = EvaluateMultiplier(ra.MultiplierExpression, ev.PayloadJson);

                    var raw = (baseHours + units * ra.PerUnitHours) * mult;

                    var factor = calibrationFactors.TryGetValue(ra.Id, out var f) ? f : 1.0m;
                    var result = raw * factor;

                    var key = (ra.ActivityId, bucketStart);
                    buckets[key] = buckets.TryGetValue(key, out var cur) ? cur + result : result;

                    contributions.Add(new Contribution(
                        ev.Id, ev.EventType, rule.Id, ra.Id, ra.ActivityId,
                        bucketStart,
                        baseHours, units, ra.PerUnitHours, mult, factor, result
                    ));
                }
            }
        }

        return (buckets, contributions);
    }

    private static DateTime AlignToInterval(DateTime utc, int minutes)
    {
        var intervalTicks = TimeSpan.FromMinutes(minutes).Ticks;
        var aligned = utc.Ticks - (utc.Ticks % intervalTicks);
        return new DateTime(aligned, DateTimeKind.Utc);
    }

    private static bool EvaluateCondition(string? expr, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(expr)) return true;

        using var doc = JsonDocument.Parse(payloadJson);
        expr = expr.Trim();

        if (expr.StartsWith("exists(", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractSingleArg(expr);
            return TryGetJsonValue(doc, path, out _);
        }

        if (expr.StartsWith("equals(", StringComparison.OrdinalIgnoreCase))
        {
            var (path, val) = ExtractTwoArgs(expr);
            if (!TryGetJsonValue(doc, path, out var el)) return false;
            return el.ValueKind == JsonValueKind.String &&
                   string.Equals(el.GetString(), TrimQuotes(val), StringComparison.OrdinalIgnoreCase);
        }

        if (expr.StartsWith("gt(", StringComparison.OrdinalIgnoreCase))
        {
            var (path, val) = ExtractTwoArgs(expr);
            if (!TryGetJsonValue(doc, path, out var el)) return false;
            if (el.ValueKind != JsonValueKind.Number) return false;
            return el.GetDecimal() > decimal.Parse(val);
        }

        return false;
    }

    private static decimal EvaluateUnits(string? expr, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0m;

        using var doc = JsonDocument.Parse(payloadJson);
        expr = expr.Trim();

        if (expr.StartsWith("count(", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractSingleArg(expr);
            if (!TryGetJsonValue(doc, path, out var el)) return 0m;
            return el.ValueKind == JsonValueKind.Array ? el.GetArrayLength() : 0m;
        }

        if (expr.StartsWith("stayNights(", StringComparison.OrdinalIgnoreCase))
        {
            var (p1, p2) = ExtractTwoArgs(expr);
            if (!TryGetJsonValue(doc, p1, out var a) || !TryGetJsonValue(doc, p2, out var b)) return 0m;
            if (a.ValueKind != JsonValueKind.String || b.ValueKind != JsonValueKind.String) return 0m;

            if (!DateTime.TryParse(a.GetString(), out var checkIn)) return 0m;
            if (!DateTime.TryParse(b.GetString(), out var checkOut)) return 0m;

            var nights = (checkOut.Date - checkIn.Date).Days;
            return Math.Max(0, nights);
        }

        return 0m;
    }

    private static decimal EvaluateMultiplier(string? expr, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 1.0m;

        using var doc = JsonDocument.Parse(payloadJson);
        expr = expr.Trim();

        if (expr.StartsWith("cabinTypeFactor(", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractSingleArg(expr);
            if (!TryGetJsonValue(doc, path, out var el) || el.ValueKind != JsonValueKind.String) return 1.0m;

            return el.GetString()?.ToLowerInvariant() switch
            {
                "deluxe" => 1.3m,
                "tent" => 0.8m,
                "standard" => 1.0m,
                _ => 1.0m
            };
        }

        if (expr.StartsWith("guestFactor(", StringComparison.OrdinalIgnoreCase))
        {
            var path = ExtractSingleArg(expr);
            if (!TryGetJsonValue(doc, path, out var el) || el.ValueKind != JsonValueKind.Number) return 1.0m;

            var guests = (int)el.GetDecimal();
            var extra = Math.Max(0, guests - 2);
            var factor = 1.0m + (decimal)extra * 0.05m;
            return Math.Max(1.0m, factor);
        }

        return 1.0m;
    }

    private static bool TryGetJsonValue(JsonDocument doc, string jsonPath, out JsonElement element)
    {
        element = default;

        jsonPath = jsonPath.Trim();
        if (!jsonPath.StartsWith("$.")) return false;

        var parts = jsonPath.Substring(2).Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonElement cur = doc.RootElement;

        foreach (var p in parts)
        {
            if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(p, out var next))
                return false;
            cur = next;
        }

        element = cur;
        return true;
    }

    private static string ExtractSingleArg(string expr)
    {
        var m = Regex.Match(expr, @"\((.+)\)");
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static (string, string) ExtractTwoArgs(string expr)
    {
        var m = Regex.Match(expr, @"\((.+)\)");
        if (!m.Success) return ("", "");
        var inner = m.Groups[1].Value;
        var parts = inner.Split(',', 2);
        if (parts.Length < 2) return ("", "");
        return (parts[0].Trim(), parts[1].Trim());
    }

    private static string TrimQuotes(string s) => s.Trim().Trim('\'').Trim('"');
}
