using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PetroVisorLite.Web.Auth;

/// <summary>
/// Pure, dependency-free JWT decoding: extracts claims from a JWT's base64url-encoded
/// payload segment without validating the signature (signature validation happens
/// server-side on every API call — the client only needs the claims to drive UI state).
/// Kept as a static, easily-unit-testable helper with no Blazor/JS-interop concerns.
/// </summary>
public static class JwtParser
{
    /// <summary>Parses the claims out of a JWT's payload. Returns an empty list if the token
    /// is malformed rather than throwing, since this only drives UI display, not security.</summary>
    public static IReadOnlyList<Claim> ParseClaims(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return Array.Empty<Claim>();
        }

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return Array.Empty<Claim>();
        }

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return Array.Empty<Claim>();
        }

        using var doc = JsonDocument.Parse(payloadBytes);
        var claims = new List<Claim>();

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    claims.Add(new Claim(property.Name, item.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }
        }

        return claims;
    }

    /// <summary>Returns true if the JWT's "exp" claim (Unix seconds) is in the past, or if no
    /// "exp" claim is present (treated as already expired, to fail closed).</summary>
    public static bool IsExpired(IReadOnlyList<Claim> claims, DateTimeOffset? now = null)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (exp is null || !long.TryParse(exp, out var expSeconds))
        {
            return true;
        }

        var expiry = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
        return expiry <= (now ?? DateTimeOffset.UtcNow);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
