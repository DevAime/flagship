using System.Text.Json;
using FlagForge.Api.Domain;

namespace FlagForge.Api.Contracts;

// ---- Auth ----
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email);

// ---- Projects & environments ----
public record CreateProjectRequest(string Key, string Name);
public record ProjectResponse(Guid Id, string Key, string Name, DateTimeOffset CreatedAt);

public record CreateEnvironmentRequest(string Key, string Name);
public record EnvironmentResponse(Guid Id, string Key, string Name, DateTimeOffset CreatedAt);

// ---- SDK keys ----
public record CreateSdkKeyRequest(SdkKeyType Type);
public record SdkKeyResponse(Guid Id, string KeyPrefix, SdkKeyType Type, DateTimeOffset CreatedAt, DateTimeOffset? RevokedAt);
public record CreatedSdkKeyResponse(SdkKeyResponse Key, string PlaintextKey);

// ---- Flags ----
public record VariationInput(string Name, JsonElement Value);
public record CreateFlagRequest(string Key, string Name, string? Description, FlagKind Kind, List<VariationInput>? Variations);
public record UpdateFlagRequest(string Name, string? Description, long ExpectedVersion);

public record VariationResponse(Guid Id, string Name, JsonElement Value);

public record FlagEnvConfigResponse(
    string EnvironmentKey,
    bool Enabled,
    Guid OffVariationId,
    Serve Fallthrough,
    List<TargetingRule> Rules,
    long Version,
    DateTimeOffset UpdatedAt);

public record FlagResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    FlagKind Kind,
    bool Archived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version,
    List<VariationResponse> Variations,
    List<FlagEnvConfigResponse> Environments);

public record EnvToggle(string EnvironmentKey, bool Enabled);
public record FlagListItem(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    FlagKind Kind,
    bool Archived,
    DateTimeOffset UpdatedAt,
    List<EnvToggle> Environments);

public record UpdateFlagConfigRequest(
    bool Enabled,
    Guid OffVariationId,
    Serve Fallthrough,
    List<TargetingRule>? Rules,
    long ExpectedVersion);

// ---- Audit ----
public record AuditEntryResponse(
    long Id,
    DateTimeOffset OccurredAt,
    string ActorEmail,
    string Action,
    string ResourceType,
    string ResourceKey,
    string? EnvironmentKey,
    JsonElement? Before,
    JsonElement? After);

public record Page<T>(List<T> Items, string? NextCursor);
