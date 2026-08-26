using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace PetroVisorLite.Web.Services;

/// <summary>
/// Attaches the JWT stored in browser <c>localStorage</c> (see
/// <see cref="Auth.JwtAuthenticationStateProvider"/>) as a Bearer token on every outgoing
/// request made through the typed <see cref="PetroVisorApiClient"/>'s <see cref="HttpClient"/>.
/// </summary>
public class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public JwtAuthorizationHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync(Auth.JwtAuthenticationStateProvider.TokenStorageKey, cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
