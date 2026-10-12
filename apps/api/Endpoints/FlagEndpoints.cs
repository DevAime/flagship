using System.Text.Json;
using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using FlagForge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Endpoints;

public static class FlagEndpoints
{
    public static RouteGroupBuilder MapFlagEndpoints(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/projects/{projectKey}/flags");

        g.MapGet("/", ListFlags);
        g.MapPost("/", CreateFlag);
        g.MapGet("/{flagKey}", GetFlag);
        g.MapPatch("/{flagKey}", UpdateFlag);
        g.MapDelete("/{flagKey}", ArchiveFlag);
        g.MapPut("/{flagKey}/environments/{environmentKey}", UpdateFlagConfig);

        return api;
    }

    // ------------------------------------------------------------------ list / get

    private static async Task<IResult> ListFlags(string projectKey, bool? includeArchived, string? search, AppDbContext db)
    {
        if (!await db.Projects.AnyAsync(p => p.Key == projectKey))
            return Results.NotFound();

        var query = db.Flags.AsNoTracking().Where(f => f.Project.Key == projectKey);

        if (includeArchived != true)
            query = query.Where(f => f.ArchivedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(f => f.Key.Contains(term) || f.Name.ToLower().Contains(term));
        }

        var flags = await query
            .Include(f => f.Configs).ThenInclude(c => c.Environment)
            .OrderBy(f => f.Key)
            .ToListAsync();

        var items = flags.Select(f => new FlagListItem(
            f.Id, f.Key, f.Name, f.Description, f.Kind, f.ArchivedAt != null, f.UpdatedAt,
            f.Configs.OrderBy(c => c.Environment.Key)
                .Select(c => new EnvToggle(c.Environment.Key, c.Enabled))
                .ToList())).ToList();

        return Results.Ok(items);
    }

    private static async Task<IResult> GetFlag(string projectKey, string flagKey, AppDbContext db)
    {
        var flag = await db.FindFlagAsync(projectKey, flagKey);
        return flag is null ? Results.NotFound() : Results.Ok(FlagMapper.ToResponse(flag));
    }

    // ------------------------------------------------------------------ create

    private static async Task<IResult> CreateFlag(
        string projectKey, CreateFlagRequest req, AppDbContext db, AuditService audit)
    {
        var project = await db.Projects.Include(p => p.Environments).SingleOrDefaultAsync(p => p.Key == projectKey);
        if (project is null) return Results.NotFound();

        var errors = new ValidationErrors();
        if (!KeyRules.IsValid(req.Key)) errors.Add("key", KeyRules.Message);
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name", "Name is required.");
        var variations = BuildVariations(req, errors);
        if (errors.Any || variations is null) return errors.ToResult();

        if (await db.Flags.AnyAsync(f => f.ProjectId == project.Id && f.Key == req.Key))
            return ApiProblems.Conflict($"Flag '{req.Key}' already exists in this project.");

        var now = DateTimeOffset.UtcNow;
        var flag = new Flag
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Key = req.Key,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Kind = req.Kind,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        foreach (var v in variations)
        {
            v.FlagId = flag.Id;
            flag.Variations.Add(v);
        }

        foreach (var env in project.Environments)
            flag.Configs.Add(FlagDefaults.NewConfig(flag, env, now));

        db.Flags.Add(flag);

        var response = FlagMapper.ToResponse(flag);
        audit.Record("flag.created", "flag", flag.Key, project.Id, null, null, response);

        await db.SaveChangesAsync();

        return Results.Created($"/api/projects/{projectKey}/flags/{flag.Key}", response);
    }

    private static List<Variation>? BuildVariations(CreateFlagRequest req, ValidationErrors errors)
    {
        List<VariationInput> inputs;

        if (req.Kind == FlagKind.Boolean && (req.Variations is null || req.Variations.Count == 0))
        {
            inputs = new List<VariationInput>
            {
                new("True", JsonUtil.True),
                new("False", JsonUtil.False)
            };
        }
        else if (req.Variations is null || req.Variations.Count < 2)
        {
            errors.Add("variations", "At least two variations are required.");
            return null;
        }
        else
        {
            inputs = req.Variations;
        }

        if (req.Kind == FlagKind.Boolean && inputs.Count != 2)
            errors.Add("variations", "Boolean flags have exactly two variations.");

        if (inputs.Count > 20)
            errors.Add("variations", "A maximum of 20 variations is allowed.");

        var list = new List<Variation>();
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];

            if (string.IsNullOrWhiteSpace(input.Name))
                errors.Add($"variations[{i}].name", "Name is required.");

            var valid = FlagRules.ValueMatchesKind(req.Kind, input.Value);
            if (!valid)
                errors.Add($"variations[{i}].value", $"Value must be a JSON value of kind {req.Kind}.");

            list.Add(new Variation
            {
                Id = Guid.NewGuid(),
                Name = input.Name?.Trim() ?? "",
                ValueJson = valid ? input.Value.GetRawText() : "null",
                Position = i
            });
        }

        return list;
    }

    // ------------------------------------------------------------------ update metadata / archive

    private static async Task<IResult> UpdateFlag(
        string projectKey, string flagKey, UpdateFlagRequest req, AppDbContext db, AuditService audit)
    {
        var flag = await db.FindFlagAsync(projectKey, flagKey);
        if (flag is null) return Results.NotFound();
        if (flag.ArchivedAt is not null) return ApiProblems.Conflict("Archived flags cannot be edited.");

        var errors = new ValidationErrors();
        if (string.IsNullOrWhiteSpace(req.Name)) errors.Add("name", "Name is required.");
        if (errors.Any) return errors.ToResult();

        if (flag.Version != req.ExpectedVersion)
            return ApiProblems.StaleVersion(flag.Version);

        var before = FlagMapper.ToResponse(flag);

        flag.Name = req.Name.Trim();
        flag.Description = req.Description?.Trim();
        flag.UpdatedAt = DateTimeOffset.UtcNow;
        flag.Version++;

        var after = FlagMapper.ToResponse(flag);
        audit.Record("flag.updated", "flag", flag.Key, flag.ProjectId, null, before, after);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiProblems.StaleVersion(null);
        }

        return Results.Ok(after);
    }

    private static async Task<IResult> ArchiveFlag(
        string projectKey, string flagKey, AppDbContext db, AuditService audit)
    {
        var flag = await db.FindFlagAsync(projectKey, flagKey);
        if (flag is null) return Results.NotFound();
        if (flag.ArchivedAt is not null) return Results.NoContent();

        var before = FlagMapper.ToResponse(flag);

        flag.ArchivedAt = DateTimeOffset.UtcNow;
        flag.UpdatedAt = flag.ArchivedAt.Value;
        flag.Version++;

        audit.Record("flag.archived", "flag", flag.Key, flag.ProjectId, null, before, FlagMapper.ToResponse(flag));

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiProblems.StaleVersion(null);
        }

        return Results.NoContent();
    }

    // ------------------------------------------------------------------ per-environment targeting

    private static async Task<IResult> UpdateFlagConfig(
        string projectKey, string flagKey, string environmentKey,
        UpdateFlagConfigRequest req, AppDbContext db, AuditService audit)
    {
        var flag = await db.FindFlagAsync(projectKey, flagKey);
        if (flag is null) return Results.NotFound();
        if (flag.ArchivedAt is not null) return ApiProblems.Conflict("Archived flags cannot be edited.");

        var config = flag.Configs.SingleOrDefault(c => c.Environment.Key == environmentKey);
        if (config is null) return Results.NotFound();

        if (config.Version != req.ExpectedVersion)
            return ApiProblems.StaleVersion(config.Version);

        var variationIds = flag.Variations.Select(v => v.Id).ToHashSet();
        var errors = new ValidationErrors();

        if (!variationIds.Contains(req.OffVariationId))
            errors.Add("offVariationId", "Unknown variation.");

        FlagRules.ValidateServe(req.Fallthrough, variationIds, "fallthrough", errors);
        var rules = FlagRules.ValidateAndNormalizeRules(req.Rules, variationIds, errors);

        if (errors.Any) return errors.ToResult();

        var before = FlagMapper.ToConfig(config);

        // Decide a readable audit action. Comparing re-serialised JSON avoids false "changed" results
        // caused by Postgres jsonb reformatting the stored text.
        var targetingChanged =
            req.OffVariationId != before.OffVariationId ||
            Targeting.ToJson(req.Fallthrough) != Targeting.ToJson(before.Fallthrough) ||
            Targeting.ToJson(rules) != Targeting.ToJson(before.Rules);

        var action = !targetingChanged && req.Enabled != before.Enabled
            ? (req.Enabled ? "flag.enabled" : "flag.disabled")
            : "flag.targeting.updated";

        config.Enabled = req.Enabled;
        config.OffVariationId = req.OffVariationId;
        config.FallthroughJson = Targeting.ToJson(req.Fallthrough);
        config.RulesJson = Targeting.ToJson(rules);
        config.UpdatedAt = DateTimeOffset.UtcNow;
        config.Version++;

        var after = FlagMapper.ToConfig(config);
        audit.Record(action, "flag", flag.Key, flag.ProjectId, environmentKey, before, after);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiProblems.StaleVersion(null);
        }

        return Results.Ok(after);
    }
}
