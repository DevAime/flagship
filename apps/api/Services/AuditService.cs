using FlagForge.Api.Auth;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;

namespace FlagForge.Api.Services;

/// <summary>
/// Records audit entries on the SAME DbContext as the change being made. Nothing is written
/// until the endpoint calls SaveChangesAsync, and EF wraps that single call in one transaction,
/// so a change and its audit entry either both commit or both roll back.
/// </summary>
public class AuditService(AppDbContext db, ICurrentUser user)
{
    public void Record(
        string action,
        string resourceType,
        string resourceKey,
        Guid? projectId,
        string? environmentKey,
        object? before,
        object? after)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = DateTimeOffset.UtcNow,
            ActorId = user.Id,
            ActorEmail = user.Email,
            Action = action,
            ResourceType = resourceType,
            ResourceKey = resourceKey,
            ProjectId = projectId,
            EnvironmentKey = environmentKey,
            BeforeJson = before is null ? null : Targeting.ToJson(before),
            AfterJson = after is null ? null : Targeting.ToJson(after)
        });
    }
}
