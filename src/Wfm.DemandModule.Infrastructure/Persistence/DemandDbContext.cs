using Microsoft.EntityFrameworkCore;
using Wfm.DemandModule.Domain.Models;

namespace Wfm.DemandModule.Infrastructure.Persistence;

public sealed class DemandDbContext : DbContext
{
    public DemandDbContext(DbContextOptions<DemandDbContext> options) : base(options) { }

    public DbSet<DataStream> DataStreams => Set<DataStream>();
    public DbSet<StreamEvent> StreamEvents => Set<StreamEvent>();
    public DbSet<WorkActivity> WorkActivities => Set<WorkActivity>();
    public DbSet<MappingVersion> MappingVersions => Set<MappingVersion>();
    public DbSet<MappingRule> MappingRules => Set<MappingRule>();
    public DbSet<MappingRuleActivity> MappingRuleActivities => Set<MappingRuleActivity>();
    public DbSet<CalibrationProfile> CalibrationProfiles => Set<CalibrationProfile>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<WorkloadBucket> WorkloadBuckets => Set<WorkloadBucket>();
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DataStream>().ToTable("DataStreams").HasKey(x => x.Id);
        b.Entity<StreamEvent>().ToTable("StreamEvents").HasKey(x => x.Id);
        b.Entity<WorkActivity>().ToTable("WorkActivities").HasKey(x => x.Id);

        b.Entity<MappingVersion>().ToTable("MappingVersions").HasKey(x => x.Id);
        b.Entity<MappingRule>().ToTable("MappingRules").HasKey(x => x.Id);
        b.Entity<MappingRuleActivity>().ToTable("MappingRuleActivities").HasKey(x => x.Id);

        b.Entity<CalibrationProfile>().ToTable("CalibrationProfiles").HasKey(x => x.Id);

        b.Entity<SimulationRun>().ToTable("SimulationRuns").HasKey(x => x.Id);
        b.Entity<WorkloadBucket>().ToTable("WorkloadBuckets").HasKey(x => x.Id);

        b.Entity<FeedbackEntry>().ToTable("FeedbackEntries").HasKey(x => x.Id);
        b.Entity<AuditLogEntry>().ToTable("AuditLogEntries").HasKey(x => x.Id);

        b.Entity<StreamEvent>().HasIndex(x => x.EventKey).IsUnique();
        b.Entity<StreamEvent>().HasIndex(x => new { x.StreamId, x.OccurredAtUtc });

        b.Entity<MappingVersion>().HasIndex(x => new { x.StreamId, x.VersionNumber }).IsUnique();
        b.Entity<MappingVersion>().HasIndex(x => new { x.StreamId, x.IsActive });

        b.Entity<WorkloadBucket>().HasIndex(x => new { x.SimulationRunId, x.ActivityId, x.IntervalStartUtc });

        base.OnModelCreating(b);
    }
}
