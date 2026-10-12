using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlagForge.Api.Domain;

// Weights are in basis points: 10000 = 100%, 2000 = 20%.
public record RolloutBucket(Guid VariationId, int Weight);

// Serve exactly one of: a fixed variation, or a percentage rollout across variations.
public record Serve(Guid? VariationId = null, List<RolloutBucket>? Rollout = null);

// Op is one of: eq, in, contains, startsWith, endsWith, gt, gte, lt, lte, exists.
public record Clause(string Attribute, string Op, List<JsonElement> Values, bool Negate = false);

// All clauses must match (AND). Rules are evaluated in order; first match wins.
public record TargetingRule(Guid Id, string? Description, List<Clause> Clauses, Serve Serve);

public static class Targeting
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Json);

    public static List<TargetingRule> ParseRules(string json) =>
        JsonSerializer.Deserialize<List<TargetingRule>>(json, Json) ?? new List<TargetingRule>();

    public static Serve ParseServe(string json) =>
        JsonSerializer.Deserialize<Serve>(json, Json) ?? new Serve();
}

public static class JsonUtil
{
    public static readonly JsonElement True = Parse("true");
    public static readonly JsonElement False = Parse("false");

    public static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public static JsonElement? ParseOrNull(string? json) =>
        json is null ? null : Parse(json);
}
