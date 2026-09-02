using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Infrastructure;

namespace PetroVisorLite.Api.Tests;

/// <summary>Tests for the /healthz liveness endpoint.</summary>
public class HealthzEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthzEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:PetroVisorDb", string.Empty);
            builder.UseSetting("IMAGE_NAME", "myregistry.azurecr.io/backend:v1.2.3");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PetroVisorDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<PetroVisorDbContext>(options =>
                    options.UseInMemoryDatabase($"HealthzTests-{Guid.NewGuid()}"));
            });
        });
    }

    [Fact]
    public async Task Healthz_Returns200WithImageName()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthzResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body!.Status);
        Assert.Equal("myregistry.azurecr.io/backend:v1.2.3", body.Image);
    }

    [Fact]
    public async Task Healthz_Returns200WithUnknownWhenImageNameNotSet()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("IMAGE_NAME", string.Empty);
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthzResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body!.Status);
        Assert.Equal("unknown", body.Image);
    }

    private sealed record HealthzResponse(string Status, string Image);
}
