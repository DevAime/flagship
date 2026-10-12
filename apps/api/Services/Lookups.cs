using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Services;

public static class Lookups
{
    public static Task<Project?> FindProjectAsync(this AppDbContext db, string projectKey) =>
        db.Projects.SingleOrDefaultAsync(p => p.Key == projectKey);

    public static Task<ProjectEnvironment?> FindEnvironmentAsync(this AppDbContext db, string projectKey, string environmentKey) =>
        db.Environments.SingleOrDefaultAsync(e => e.Key == environmentKey && e.Project.Key == projectKey);

    public static Task<Flag?> FindFlagAsync(this AppDbContext db, string projectKey, string flagKey) =>
        db.Flags
            .Include(f => f.Variations)
            .Include(f => f.Configs).ThenInclude(c => c.Environment)
            .AsSplitQuery()
            .SingleOrDefaultAsync(f => f.Project.Key == projectKey && f.Key == flagKey);
}
