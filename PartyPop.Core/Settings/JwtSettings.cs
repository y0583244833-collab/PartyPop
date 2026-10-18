namespace PartyPop.Core.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>לפחות 32 תווים. נשמר ב-User Secrets.</summary>
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
