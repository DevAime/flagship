namespace FlagForge.Api.Auth;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "flagforge";
    public string Audience { get; set; } = "flagforge-dashboard";
    public int ExpiryMinutes { get; set; } = 60;
}
