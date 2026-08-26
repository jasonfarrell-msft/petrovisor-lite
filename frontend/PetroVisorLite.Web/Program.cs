using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PetroVisorLite.Web;
using PetroVisorLite.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base URL for the backend API comes from wwwroot/appsettings.json (and
// appsettings.Development.json), never hardcoded, so this static-hosted frontend can point
// at any backend deployment without a rebuild.
var backendBaseUrl = builder.Configuration["BackendApi:BaseUrl"]
    ?? throw new InvalidOperationException("Configuration value 'BackendApi:BaseUrl' is required.");

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddHttpClient<PetroVisorApiClient>(client =>
    {
        client.BaseAddress = new Uri(backendBaseUrl);
    });

builder.Services.AddScoped<ChartInteropService>();

await builder.Build().RunAsync();
