namespace FlagForge.Api.Domain;

public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class Project
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public List<ProjectEnvironment> Environments { get; set; } = new();
    public List<Flag> Flags { get; set; } = new();
}

public class ProjectEnvironment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public List<SdkKey> SdkKeys { get; set; } = new();
}

public enum SdkKeyType
{
    Server,
    Client
}

public class SdkKey
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public SdkKeyType Type { get; set; }

    /// <summary>First characters of the key, safe to display in the dashboard.</summary>
    public string KeyPrefix { get; set; } = "";

    /// <summary>SHA-256 of the full key. The plaintext key is shown once and never stored.</summary>
    public string KeyHash { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public ProjectEnvironment Environment { get; set; } = null!;
}

public enum FlagKind
{
    Boolean,
    String,
    Number,
    Json
}

public class Flag
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public FlagKind Kind { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token, incremented on every change.</summary>
    public long Version { get; set; }

    public Project Project { get; set; } = null!;
    public List<Variation> Variations { get; set; } = new();
    public List<FlagEnvironmentConfig> Configs { get; set; } = new();
}

public class Variation
{
    public Guid Id { get; set; }
    public Guid FlagId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Raw JSON value served to the application (true, "blue", 42, {...}).</summary>
    public string ValueJson { get; set; } = "null";

    public int Position { get; set; }
}

/// <summary>Per-environment targeting for one flag: on/off, rules, default serve.</summary>
public class FlagEnvironmentConfig
{
    public Guid Id { get; set; }
    public Guid FlagId { get; set; }
    public Guid EnvironmentId { get; set; }
    public bool Enabled { get; set; }

    /// <summary>Variation served when the flag is off.</summary>
    public Guid OffVariationId { get; set; }

    /// <summary>JSON-serialised <see cref="Serve"/>: what to serve when no rule matches.</summary>
    public string FallthroughJson { get; set; } = "{}";

    /// <summary>JSON-serialised list of <see cref="TargetingRule"/>, evaluated top to bottom.</summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>Incremented on every change. Becomes the ruleset version/ETag SDKs use.</summary>
    public long Version { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Flag Flag { get; set; } = null!;
    public ProjectEnvironment Environment { get; set; } = null!;
}

/// <summary>Append-only record of every change made through the API.</summary>
public class AuditEntry
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorEmail { get; set; } = "";
    public string Action { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public string ResourceKey { get; set; } = "";
    public Guid? ProjectId { get; set; }
    public string? EnvironmentKey { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
