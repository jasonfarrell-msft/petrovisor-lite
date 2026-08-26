using PetroVisorLite.Web.Auth;

namespace PetroVisorLite.Web.Tests;

public class JwtParserTests
{
    private static string CreateJwt(object payload)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\"}"u8.ToArray());
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var payloadEncoded = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));
        return $"{header}.{payloadEncoded}.signature";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Fact]
    public void ParseClaims_ExtractsSimpleStringClaims()
    {
        var jwt = CreateJwt(new { sub = "user-1", unique_name = "jane@example.com", exp = 9999999999 });

        var claims = JwtParser.ParseClaims(jwt);

        Assert.Contains(claims, c => c.Type == "sub" && c.Value == "user-1");
        Assert.Contains(claims, c => c.Type == "unique_name" && c.Value == "jane@example.com");
    }

    [Fact]
    public void ParseClaims_ExpandsArrayClaimsIntoMultipleClaims()
    {
        var jwt = CreateJwt(new { role = new[] { "Engineer", "Viewer" } });

        var claims = JwtParser.ParseClaims(jwt).Where(c => c.Type == "role").ToList();

        Assert.Equal(2, claims.Count);
        Assert.Contains(claims, c => c.Value == "Engineer");
        Assert.Contains(claims, c => c.Value == "Viewer");
    }

    [Fact]
    public void ParseClaims_ReturnsEmpty_ForMalformedToken()
    {
        var claims = JwtParser.ParseClaims("not-a-jwt");

        Assert.Empty(claims);
    }

    [Fact]
    public void ParseClaims_ReturnsEmpty_ForEmptyString()
    {
        var claims = JwtParser.ParseClaims(string.Empty);

        Assert.Empty(claims);
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpClaimInPast()
    {
        var jwt = CreateJwt(new { exp = 1000000000 }); // year 2001
        var claims = JwtParser.ParseClaims(jwt);

        Assert.True(JwtParser.IsExpired(claims));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenExpClaimInFuture()
    {
        var future = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var jwt = CreateJwt(new { exp = future });
        var claims = JwtParser.ParseClaims(jwt);

        Assert.False(JwtParser.IsExpired(claims));
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenNoExpClaim()
    {
        var jwt = CreateJwt(new { sub = "user-1" });
        var claims = JwtParser.ParseClaims(jwt);

        Assert.True(JwtParser.IsExpired(claims));
    }
}
