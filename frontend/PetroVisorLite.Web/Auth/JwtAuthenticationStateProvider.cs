using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace PetroVisorLite.Web.Auth;

/// <summary>
/// Client-side <see cref="AuthenticationStateProvider"/> for the Blazor WebAssembly app.
/// There is no server-side cookie/session here — the source of truth is the JWT issued by
/// the backend's <c>/api/auth/login</c> endpoint, persisted in browser <c>localStorage</c>
/// (via <see cref="ILocalStorageService"/>) and decoded (via <see cref="JwtParser"/>, no
/// signature validation needed client-side — the backend validates the signature on every
/// API call) into the <see cref="ClaimsPrincipal"/> that drives <c>&lt;AuthorizeView&gt;</c>
/// and <c>[Authorize]</c> across the app.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    public const string TokenStorageKey = "petrovisor_jwt";

    private readonly ILocalStorageService _localStorage;
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync(TokenStorageKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        var claims = JwtParser.ParseClaims(token);
        if (JwtParser.IsExpired(claims))
        {
            await _localStorage.RemoveItemAsync(TokenStorageKey);
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, "jwt", nameType: "unique_name", roleType: "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Persists a freshly-issued JWT and notifies Blazor that auth state changed
    /// (so <c>&lt;AuthorizeView&gt;</c> re-renders immediately after login).</summary>
    public async Task NotifyLoginAsync(string jwt)
    {
        await _localStorage.SetItemAsStringAsync(TokenStorageKey, jwt);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>Clears the stored JWT and notifies Blazor that auth state changed (logout).</summary>
    public async Task NotifyLogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenStorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
