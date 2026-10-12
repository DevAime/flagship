using System.Text.RegularExpressions;

namespace FlagForge.Api.Contracts;

public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool Any => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            _errors[field] = list;
        }
        list.Add(message);
    }

    public IResult ToResult() =>
        Results.ValidationProblem(_errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
}

public static class KeyRules
{
    private static readonly Regex Pattern = new("^[a-z0-9][a-z0-9._-]{0,62}$", RegexOptions.Compiled);

    public const string Message =
        "Key must be 1-63 characters: lowercase letters, digits, '.', '_' or '-', starting with a letter or digit.";

    public static bool IsValid(string? key) => key is not null && Pattern.IsMatch(key);
}

public static class ApiProblems
{
    public static IResult Conflict(string detail) =>
        Results.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");

    public static IResult StaleVersion(long? currentVersion) =>
        Results.Problem(
            detail: currentVersion is null
                ? "This resource was modified by someone else. Reload it and try again."
                : $"This resource is now at version {currentVersion}. Reload it and try again.",
            statusCode: StatusCodes.Status409Conflict,
            title: "Stale version");
}
