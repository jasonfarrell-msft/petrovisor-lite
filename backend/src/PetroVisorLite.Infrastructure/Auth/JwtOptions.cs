using System.Security.Cryptography;
using System.Text;

namespace PetroVisorLite.Infrastructure.Auth;

/// <summary>
/// JWT signing configuration. Bind from configuration section "Jwt".
/// The signing key MUST come from User Secrets locally
/// (dotnet user-secrets set "Jwt:Key" "...") or an environment variable /
/// Azure Key Vault (via Managed Identity) in Azure — never hardcode it here
/// or commit it to appsettings.json.
///
/// When no value is configured (for example in local dev or test runs), a stable
/// in-memory fallback key is used so the login flow and token validation both agree
/// without requiring a developer to set secrets before the app starts.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    private static readonly byte[] FallbackKeyBytes = RandomNumberGenerator.GetBytes(32);
    private static int _fallbackWarningLogged;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PetroVisorLite";
    public string Audience { get; set; } = "PetroVisorLiteClients";
    public int ExpiryMinutes { get; set; } = 60;

    public static byte[] GetEffectiveKeyBytes(string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            if (Interlocked.CompareExchange(ref _fallbackWarningLogged, 1, 0) == 0)
            {
                Console.WriteLine(
                    "Warning: Jwt:Key is not configured. Using a process-local fallback signing key. " +
                    "Set Jwt:Key via user-secrets or environment variables to avoid token validation failures across restarts.");
            }

            return FallbackKeyBytes;
        }

        return Encoding.UTF8.GetBytes(configuredKey);
    }
}
