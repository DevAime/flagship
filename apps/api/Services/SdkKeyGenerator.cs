using System.Security.Cryptography;
using System.Text;
using FlagForge.Api.Domain;

namespace FlagForge.Api.Services;

public static class SdkKeyGenerator
{
    public static string Generate(SdkKeyType type)
    {
        var prefix = type == SdkKeyType.Server ? "srv_" : "cli_";
        return prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    }

    // SHA-256 is fine here: the key is 192 bits of randomness, so there is nothing to brute-force.
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
}
