using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Infrastructure;

namespace PetroVisorLite.Api.Tests;

public class FacilityComparisonEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FacilityComparisonEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:PetroVisorDb", string.Empty);

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PetroVisorDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PetroVisorDbContext>(options =>
                    options.UseInMemoryDatabase($"FacilityComparisonTests-{Guid.NewGuid()}"));
            });
        });
    }

    [Fact]
    public async Task FacilitiesComparisonEndpoint_WithInvalidDateRange_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/facilities/comparison?rangeStart=2024-02-01&rangeEnd=2024-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("rangeStart must be on or before rangeEnd.", payload!["message"]);
    }
}
