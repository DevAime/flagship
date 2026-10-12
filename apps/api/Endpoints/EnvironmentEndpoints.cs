using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using FlagForge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Endpoints;

public static class EnvironmentEndpoints
{
    public static RouteGroupBuilder MapEnvironmentEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/projects/{projectKey}/environments");

        g.MapGet("/", ListEnvironments);
        g.MapPost("/", CreateEnvironment);

        return api;
    }

    private static async Task<IResult> ListEnvironments(string projectKey, AppDbContext db)
    {
        if (!await db.Projects.AnyAsync(p => p.Key == projectKey))
            return Results.NotFound();

        var envs = await db.Environments.AsNoTracking()
            .Where(e => e.Project.Key == projectKey)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new EnvironmentResponse(e.Id, e.Key, e.Name, e.CreatedAt))
            .ToListAsync();

        return Results.Ok(envs);
    }

    private static async Task<IResult> CreateEnvironment(
        string projectKey, CreateEnvironmentRequest req, AppDbContext db, AuditService audit)
    {
        var project = await db.FindProjectAsync(projectKey);
        if (project is null) return Results.NotFound();

        var errors = new ValidationErrors();
        if (!KeyRules.IsValid(req.Key)) errors.Add("key", KeyRules.Message);
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name", "Name is required.");
        if (errors.Any) return errors.ToResult();

        if (await db.Environments.AnyAsync(e => e.ProjectId == project.Id && e.Key == req.Key))
            return ApiProblems.Conflict($"Environment '{req.Key}' already exists in this project.");

        var now = DateTimeOffset.UtcNow;
        var env = new ProjectEnvironment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Key = req.Key,
            Name = req.Name.Trim(),
            CreatedAt = now
        };
        db.Environments.Add(env);

        // Existing flags need a (disabled) config in the new environment.
        var flags = await db.Flags.Include(f => f.Variations).Where(f => f.ProjectId == project.Id).ToListAsync();
        foreach (var flag in flags)
            db.FlagConfigs.Add(FlagDefaults.NewConfig(flag, env, now));

        var response = new EnvironmentResponse(env.Id, env.Key, env.Name, env.CreatedAt);
        audit.Record("environment.created", "environment", env.Key, project.Id, env.Key, null, response);

        await db.SaveChangesAsync();

        return Results.Created($"/api/projects/{projectKey}/environments/{env.Key}", response);
    }
}
