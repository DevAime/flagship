using System.Text.Json;
using FlagForge.Api.Contracts;
using FlagForge.Api.Domain;

namespace FlagForge.Api.Services;

public static class FlagRules
{
    private static readonly HashSet<string> Operators = new()
    {
        "eq", "in", "contains", "startsWith", "endsWith", "gt", "gte", "lt", "lte", "exists"
    };

    private static readonly HashSet<string> NumericOperators = new() { "gt", "gte", "lt", "lte" };

    public static bool ValueMatchesKind(FlagKind kind, JsonElement v) => kind switch
    {
        FlagKind.Boolean => v.ValueKind is JsonValueKind.True or JsonValueKind.False,
        FlagKind.String => v.ValueKind == JsonValueKind.String,
        FlagKind.Number => v.ValueKind == JsonValueKind.Number,
        FlagKind.Json => v.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
        _ => false
    };

    public static void ValidateServe(Serve? serve, HashSet<Guid> variationIds, string path, ValidationErrors errors)
    {
        if (serve is null)
        {
            errors.Add(path, "Required.");
            return;
        }

        var hasVariation = serve.VariationId is not null;
        var hasRollout = serve.Rollout is { Count: > 0 };

        if (hasVariation == hasRollout)
        {
            errors.Add(path, "Provide exactly one of 'variationId' or 'rollout'.");
            return;
        }

        if (hasVariation)
        {
            if (!variationIds.Contains(serve.VariationId!.Value))
                errors.Add($"{path}.variationId", "Unknown variation.");
            return;
        }

        var total = 0;
        foreach (var bucket in serve.Rollout!)
        {
            if (!variationIds.Contains(bucket.VariationId))
                errors.Add($"{path}.rollout", "Rollout references an unknown variation.");
            if (bucket.Weight < 0)
                errors.Add($"{path}.rollout", "Weights cannot be negative.");
            total += bucket.Weight;
        }

        if (total != 10_000)
            errors.Add($"{path}.rollout", "Weights must add up to 10000 (100%).");
    }

    public static List<TargetingRule> ValidateAndNormalizeRules(
        List<TargetingRule>? rules, HashSet<Guid> variationIds, ValidationErrors errors)
    {
        var result = new List<TargetingRule>();
        if (rules is null) return result;

        if (rules.Count > 50)
        {
            errors.Add("rules", "A maximum of 50 rules is allowed.");
            return result;
        }

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var path = $"rules[{i}]";

            if (rule is null)
            {
                errors.Add(path, "Rule is required.");
                continue;
            }

            if (rule.Clauses is null || rule.Clauses.Count == 0)
            {
                errors.Add($"{path}.clauses", "At least one clause is required.");
            }
            else
            {
                for (var j = 0; j < rule.Clauses.Count; j++)
                    ValidateClause(rule.Clauses[j], $"{path}.clauses[{j}]", errors);
            }

            ValidateServe(rule.Serve, variationIds, $"{path}.serve", errors);

            result.Add(rule with { Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id });
        }

        return result;
    }

    private static void ValidateClause(Clause? clause, string path, ValidationErrors errors)
    {
        if (clause is null)
        {
            errors.Add(path, "Clause is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(clause.Attribute))
            errors.Add($"{path}.attribute", "Attribute is required.");

        if (!Operators.Contains(clause.Op))
        {
            errors.Add($"{path}.op", $"Operator must be one of: {string.Join(", ", Operators)}.");
            return;
        }

        if (clause.Op == "exists") return;

        if (clause.Values is null || clause.Values.Count == 0)
        {
            errors.Add($"{path}.values", "At least one value is required.");
            return;
        }

        if (NumericOperators.Contains(clause.Op) && clause.Values.Any(v => v.ValueKind != JsonValueKind.Number))
            errors.Add($"{path}.values", "Numeric operators need number values.");
    }
}
