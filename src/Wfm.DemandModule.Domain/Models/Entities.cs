namespace Wfm.DemandModule.Domain.Models;

public sealed class DataStream
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string SourceSystem { get; set; } = "";
    public string Industry { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StreamEvent
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public string EventKey { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WorkActivity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class MappingVersion
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public int VersionNumber { get; set; }
    public string Name { get; set; } = "";
    public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class MappingRule
{
    public Guid Id { get; set; }
    public Guid MappingVersionId { get; set; }
    public string EventType { get; set; } = "";
    public string? ConditionExpression { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class MappingRuleActivity
{
    public Guid Id { get; set; }
    public Guid MappingRuleId { get; set; }
    public Guid ActivityId { get; set; }
    public decimal BaseHours { get; set; }
    public string? UnitExpression { get; set; }
    public decimal PerUnitHours { get; set; }
    public string? MultiplierExpression { get; set; }
}

public sealed class CalibrationProfile
{
    public Guid Id { get; set; }
    public Guid MappingVersionId { get; set; }
    public Guid RuleActivityId { get; set; }
    public decimal Factor { get; set; } = 1.0m;
    public decimal Lambda { get; set; } = 0.1m;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SimulationRun
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public Guid MappingVersionId { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int IntervalMinutes { get; set; }
    public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WorkloadBucket
{
    public Guid Id { get; set; }
    public Guid SimulationRunId { get; set; }
    public Guid ActivityId { get; set; }
    public DateTime IntervalStartUtc { get; set; }
    public DateTime IntervalEndUtc { get; set; }
    public decimal Hours { get; set; }
    public string ExplanationJson { get; set; } = "{}";
}

public sealed class FeedbackEntry
{
    public Guid Id { get; set; }
    public Guid MappingVersionId { get; set; }
    public Guid RuleActivityId { get; set; }
    public DateTime IntervalStartUtc { get; set; }
    public decimal ActualHours { get; set; }
    public string? Comment { get; set; }
    public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditLogEntry
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string ActorUserId { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string DetailsJson { get; set; } = "{}";
}
