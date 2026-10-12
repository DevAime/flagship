using FlagForge.Api.Domain;

namespace FlagForge.Api.Services;

public static class FlagDefaults
{
    /// <summary>
    /// A new flag starts OFF in every environment. Off serves variation #2 (false for booleans);
    /// when on with no rules it serves variation #1 (true for booleans).
    /// </summary>
    public static FlagEnvironmentConfig NewConfig(Flag flag, ProjectEnvironment env, DateTimeOffset now)
    {
        var ordered = flag.Variations.OrderBy(v => v.Position).ToList();

        return new FlagEnvironmentConfig
        {
            Id = Guid.NewGuid(),
            FlagId = flag.Id,
            EnvironmentId = env.Id,
            Environment = env,
            Enabled = false,
            OffVariationId = ordered[1].Id,
            FallthroughJson = Targeting.ToJson(new Serve(VariationId: ordered[0].Id)),
            RulesJson = "[]",
            Version = 1,
            UpdatedAt = now
        };
    }
}
