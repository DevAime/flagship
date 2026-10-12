using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using FlagForge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/projects");

        g.MapGet("/", ListProjects);
        g.MapPost("/", CreateProject);
        g.MapGet("/{projectKey}", GetProject);

        return api;
    }

    private static async Task<IResult> ListProjects(AppDbContext db)
    {
        var projects = await db.Projects.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProjectResponse(p.Id, p.Key, p.Name, p.CreatedAt))
            .ToListAsync();

        return Results.Ok(projects);
    }

    private static async Task<IResult> GetProject(string projectKey, AppDbContext db)
    {
        var p = await db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Key == projectKey);
        return p is null
            ? Results.NotFound()
            : Results.Ok(new ProjectResponse(p.Id, p.Key, p.Name, p.CreatedAt));
    }

    private static async Task<IResult> CreateProject(CreateProjectRequest req, AppDbContext db, AuditService audit)
    {
        var errors = new ValidationErrors();
        if (!KeyRules.IsValid(req.Key)) errors.Add("key", KeyRules.Message);
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name", "Name is required.");
        if (errors.Any) return errors.ToResult();

        if (await db.Projects.AnyAsync(p => p.Key == req.Key))
            return ApiProblems.Conflict($"A project with key '{req.Key}' already exists.");

        var now = DateTimeOffset.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Key = req.Key,
            Name = req.Name.Trim(),
            CreatedAt = now
        };

        // Every project starts with two environments.
        project.Environments.Add(new ProjectEnvironment { Id = Guid.NewGuid(), Key = "development", Name = "Development", CreatedAt = now });
        project.Environments.Add(new ProjectEnvironment { Id = Guid.NewGuid(), Key = "production", Name = "Production", CreatedAt = now });

        db.Projects.Add(project);

        var response = new ProjectResponse(project.Id, project.Key, project.Name, project.CreatedAt);
        audit.Record("project.created", "project", project.Key, project.Id, null, null, response);

        await db.SaveChangesAsync();   // project + environments + audit entry: one transaction

        return Results.Created($"/api/projects/{project.Key}", response);
    }
}
