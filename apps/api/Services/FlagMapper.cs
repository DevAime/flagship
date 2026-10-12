using FlagForge.Api.Contracts;
using FlagForge.Api.Domain;

namespace FlagForge.Api.Services;

public static class FlagMapper
{
    public static FlagResponse ToResponse(Flag f) => new(
        f.Id, f.Key, f.Name, f.Description, f.Kind, f.ArchivedAt != null,
        f.CreatedAt, f.UpdatedAt, f.Version,
        f.Variations.OrderBy(v => v.Position).Select(ToVariation).ToList(),
        f.Configs.OrderBy(c => c.Environment.Key).Select(ToConfig).ToList());

    public static VariationResponse ToVariation(Variation v) =>
        new(v.Id, v.Name, JsonUtil.Parse(v.ValueJson));

    public static FlagEnvConfigResponse ToConfig(FlagEnvironmentConfig c) => new(
        c.Environment.Key,
        c.Enabled,
        c.OffVariationId,
        Targeting.ParseServe(c.FallthroughJson),
        Targeting.ParseRules(c.RulesJson),
        c.Version,
        c.UpdatedAt);
}
