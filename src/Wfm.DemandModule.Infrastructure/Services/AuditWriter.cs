using System.Text.Json;
using Wfm.DemandModule.Domain.Models;
using Wfm.DemandModule.Infrastructure.Persistence;

namespace Wfm.DemandModule.Infrastructure.Services;

public interface IAuditWriter
{
    Task WriteAsync(string actorUserId, string actorRole, string action, string entityType, string entityId, object details, CancellationToken ct);
}

public sealed class AuditWriter : IAuditWriter
{
    private readonly DemandDbContext _db;

    public AuditWriter(DemandDbContext db) => _db = db;

    public async Task WriteAsync(string actorUserId, string actorRole, string action, string entityType, string entityId, object details, CancellationToken ct)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(details),
            OccurredAtUtc = DateTime.UtcNow
        };

        _db.AuditLogEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
