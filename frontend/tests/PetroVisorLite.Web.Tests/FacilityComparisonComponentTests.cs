using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Web.Pages;
using PetroVisorLite.Web.Services;

namespace PetroVisorLite.Web.Tests;

public class FacilityComparisonComponentTests : TestContext
{
    [Fact]
    public void FacilityComparisonPage_RendersFacilitiesInDefaultOrder()
    {
        var apiClient = CreateApiClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "rangeStart": "2024-01-01",
              "rangeEnd": "2024-01-31",
              "facilities": [
                { "facilityId": "d1f4d7f2-2a64-4a8d-97d5-9a4f7e9d2d88", "facilityName": "Zulu", "facilityType": "Battery", "tiedInWellCount": 2, "totalOilBbl": 900, "totalGasMcf": 100, "totalWaterBbl": 10, "hasNoReportedProduction": false },
                { "facilityId": "9f3a11aa-a2ad-4f14-bf5b-8a3d9f9613a2", "facilityName": "Alpha", "facilityType": "Battery", "tiedInWellCount": 0, "totalOilBbl": 0, "totalGasMcf": 0, "totalWaterBbl": 0, "hasNoReportedProduction": true }
              ]
            }
            """, Encoding.UTF8, "application/json")
        });

        Services.AddSingleton(apiClient);

        var cut = RenderComponent<FacilityComparison>();

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Zulu", rows[0].TextContent);
        Assert.Contains("Alpha", rows[1].TextContent);
        Assert.Contains("No reported production", cut.Markup);
    }

    [Fact]
    public void FacilityComparisonPage_ShowsFriendlyErrorWhenApiUnreachable()
    {
        var apiClient = CreateApiClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Services.AddSingleton(apiClient);

        var cut = RenderComponent<FacilityComparison>();

        Assert.Contains("Could not reach the backend API", cut.Markup);
    }

    private static PetroVisorApiClient CreateApiClient(HttpResponseMessage comparisonResponse)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/api/facilities/comparison"))
            {
                return comparisonResponse;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        return new PetroVisorApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request));
    }
}
