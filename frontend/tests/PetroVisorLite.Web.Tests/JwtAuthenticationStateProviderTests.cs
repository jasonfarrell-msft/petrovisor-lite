using PetroVisorLite.Web.Auth;

namespace PetroVisorLite.Web.Tests;

/// <summary>
/// Exercises <see cref="JwtAuthenticationStateProvider"/> against a lightweight in-memory
/// <see cref="FakeLocalStorageService"/> stand-in (see that class for rationale — this keeps
/// the tests fast, plain xUnit, and independent of a real browser/JS interop).
/// </summary>
public class JwtAuthenticationStateProviderTests
{
    private static string CreateJwt(long expUnixSeconds, string role = "Viewer") =>
        CreateJwt(new { unique_name = "jane@example.com", role, exp = expUnixSeconds });

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
    public async Task GetAuthenticationStateAsync_ReturnsAnonymous_WhenNoTokenStored()
    {
        var provider = new JwtAuthenticationStateProvider(new FakeLocalStorageService());

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task NotifyLoginAsync_PersistsToken_AndSubsequentStateIsAuthenticated()
    {
        var provider = new JwtAuthenticationStateProvider(new FakeLocalStorageService());
        var jwt = CreateJwt(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(), role: "Engineer");

        await provider.NotifyLoginAsync(jwt);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.True(state.User.IsInRole("Engineer"));
    }

    [Fact]
    public async Task NotifyLogoutAsync_ClearsToken_AndSubsequentStateIsAnonymous()
    {
        var localStorage = new FakeLocalStorageService();
        var provider = new JwtAuthenticationStateProvider(localStorage);
        var jwt = CreateJwt(DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());
        await provider.NotifyLoginAsync(jwt);

        await provider.NotifyLogoutAsync();
        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAnonymous_WhenTokenExpired()
    {
        var provider = new JwtAuthenticationStateProvider(new FakeLocalStorageService());
        var expiredJwt = CreateJwt(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds());
        await provider.NotifyLoginAsync(expiredJwt);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }
}
