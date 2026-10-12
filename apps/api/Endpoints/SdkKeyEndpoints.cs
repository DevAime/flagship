using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using FlagForge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Endpoints;

public static class SdkKeyEndpoints
{
    public static RouteGroupBuilder MapSdkKeyEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/projects/{projectKey}/environments/{environmentKey}/sdk-keys");

        g.MapGet("/", ListKeys);
        g.MapPost("/", CreateKey);
        g.MapDelete("/{id:guid}", RevokeKey);

        return api;
    }

    private static async Task<IResult> ListKeys(string projectKey, string environmentKey, AppDbContext db)
    {
        var env = await db.FindEnvironmentAsync(projectKey, environmentKey);
        if (env is null) return Results.NotFound();

        var keys = await db.SdkKeys.AsNoTracking()
            .Where(k => k.EnvironmentId == env.Id)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new SdkKeyResponse(k.Id, k.KeyPrefix, k.Type, k.CreatedAt, k.RevokedAt))
            .ToListAsync();

        return Results.Ok(keys);
    }

    private static async Task<IResult> CreateKey(
        string projectKey, string environmentKey, CreateSdkKeyRequest req, AppDbContext db, AuditService audit)
    {
        var env = await db.FindEnvironmentAsync(projectKey, environmentKey);
        if (env is null) return Results.NotFound();

        if (!Enum.IsDefined(req.Type))
        {
            var errors = new ValidationErrors();
            errors.Add("type", "Type must be 'Server' or 'Client'.");
            return errors.ToResult();
        }

        var plaintext = SdkKeyGenerator.Generate(req.Type);
        var entity = new SdkKey
        {
            Id = Guid.NewGuid(),
            EnvironmentId = env.Id,
            Type = req.Type,
            KeyPrefix = plaintext[..8],
            KeyHash = SdkKeyGenerator.Hash(plaintext),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.SdkKeys.Add(entity);

        var response = new SdkKeyResponse(entity.Id, entity.KeyPrefix, entity.Type, entity.CreatedAt, null);

        // The audit entry stores the prefix only. Never put the plaintext key in the log.
        audit.Record("sdk_key.created", "sdk_key", entity.KeyPrefix, env.ProjectId, env.Key, null, response);

        await db.SaveChangesAsync();

        // The only time the plaintext key is ever returned.
        return Results.Created(
            $"/api/projects/{projectKey}/environments/{environmentKey}/sdk-keys/{entity.Id}",
            new CreatedSdkKeyResponse(response, plaintext));
    }

    private static async Task<IResult> RevokeKey(
        string projectKey, string environmentKey, Guid id, AppDbContext db, AuditService audit)
    {
        var env = await db.FindEnvironmentAsync(projectKey, environmentKey);
        if (env is null) return Results.NotFound();

        var key = await db.SdkKeys.SingleOrDefaultAsync(k => k.Id == id && k.EnvironmentId == env.Id);
        if (key is null) return Results.NotFound();
        if (key.RevokedAt is not null) return Results.NoContent();

        var before = new SdkKeyResponse(key.Id, key.KeyPrefix, key.Type, key.CreatedAt, null);
        key.RevokedAt = DateTimeOffset.UtcNow;
        var after = new SdkKeyResponse(key.Id, key.KeyPrefix, key.Type, key.CreatedAt, key.RevokedAt);

        audit.Record("sdk_key.revoked", "sdk_key", key.KeyPrefix, env.ProjectId, env.Key, before, after);

        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
