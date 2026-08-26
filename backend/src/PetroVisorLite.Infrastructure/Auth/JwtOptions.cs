namespace PetroVisorLite.Infrastructure.Auth;

/// <summary>
/// JWT signing configuration. Bind from configuration section "Jwt".
/// The signing key MUST come from User Secrets locally
/// (dotnet user-secrets set "Jwt:Key" "...") or an environment variable /
/// Azure Key Vault (via Managed Identity) in Azure — never hardcode it here
/// or commit it to appsettings.json.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PetroVisorLite";
    public string Audience { get; set; } = "PetroVisorLiteClients";
    public int ExpiryMinutes { get; set; } = 60;
}
