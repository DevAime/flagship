using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Endpoints;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder api)
    {
        // Read-only on purpose: there is no way to edit or delete audit entries through the API.
        api.MapGet("/projects/{projectKey}/audit", ListAudit);
        return api;
    }

    private static async Task<IResult> ListAudit(
        string projectKey, string? flag, string? action, int? limit, string? cursor, AppDbContext db)
    {
        var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(p => p.Key == projectKey);
        if (project is null) return Results.NotFound();

        var take = Math.Clamp(limit ?? 50, 1, 200);

        var query = db.AuditEntries.AsNoTracking().Where(a => a.ProjectId == project.Id);

        if (!string.IsNullOrWhiteSpace(flag))
            query = query.Where(a => a.ResourceType == "flag" && a.ResourceKey == flag);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        // Keyset pagination: the cursor is the Id of the last entry on the previous page.
        if (long.TryParse(cursor, out var beforeId))
            query = query.Where(a => a.Id < beforeId);

        var rows = await query.OrderByDescending(a => a.Id).Take(take + 1).ToListAsync();

        var items = rows.Take(take).Select(a => new AuditEntryResponse(
            a.Id, a.OccurredAt, a.ActorEmail, a.Action, a.ResourceType, a.ResourceKey, a.EnvironmentKey,
            JsonUtil.ParseOrNull(a.BeforeJson), JsonUtil.ParseOrNull(a.AfterJson))).ToList();

        var nextCursor = rows.Count > take ? items[^1].Id.ToString() : null;

        return Results.Ok(new Page<AuditEntryResponse>(items, nextCursor));
    }
}
