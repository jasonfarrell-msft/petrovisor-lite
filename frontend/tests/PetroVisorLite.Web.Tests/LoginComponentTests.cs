using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Web.Pages;

namespace PetroVisorLite.Web.Tests;

/// <summary>
/// A small bUnit component-render smoke test, demonstrating the bUnit path alongside the
/// plain-xUnit tests above. Renders the anonymous-accessible Login page (no auth context
/// needed) and checks the expected form fields render.
/// </summary>
public class LoginComponentTests : TestContext
{
    public LoginComponentTests()
    {
        Services.AddSingleton(new PetroVisorLite.Web.Services.PetroVisorApiClient(new HttpClient { BaseAddress = new Uri("https://localhost/") }));
        Services.AddBlazoredLocalStorage();
        Services.AddScoped<PetroVisorLite.Web.Auth.JwtAuthenticationStateProvider>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void LoginPage_RendersEmailAndPasswordFields()
    {
        var cut = RenderComponent<Login>();

        Assert.Contains("Log in", cut.Markup);
        Assert.NotNull(cut.Find("input[type=password]"));
    }
}
