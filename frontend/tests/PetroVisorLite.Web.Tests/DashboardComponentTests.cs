using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Web.Pages;
using PetroVisorLite.Web.Services;

namespace PetroVisorLite.Web.Tests;

public class DashboardComponentTests : TestContext
{
    public DashboardComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<ChartInteropService>();
    }

    [Fact]
    public void DashboardPage_ShowsFriendlyUnsupportedAssistantMessage()
    {
        var apiClient = CreateApiClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "wellCount": 3,
                "facilityCount": 2,
                "totalOilBbl30d": 1000,
                "totalGasMcf30d": 500,
                "fieldDailyProduction": [],
                "artificialLiftBreakdown": [],
                "topWellsByDecline": []
            }
            """, Encoding.UTF8, "application/json")
        }, new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "intent": 0,
                "isSupported": false,
                "message": "I can't answer that yet.",
                "data": {}
            }
            """, Encoding.UTF8, "application/json")
        });

        Services.AddSingleton(apiClient);

        var cut = RenderComponent<Dashboard>();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Ask PetroVisor")).Click();
        cut.Find("aside input[type=text]").Input("Can you explain the weather?");
        cut.FindAll("aside button").Last().Click();

        Assert.Contains("I can't answer that yet.", cut.Markup);
        Assert.Contains("Try asking about top wells by decline rate", cut.Markup);
    }

    [Fact]
    public void DashboardPage_RendersSupportedDeclineIntentInChatPanel()
    {
        var apiClient = CreateApiClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "wellCount": 3,
                "facilityCount": 2,
                "totalOilBbl30d": 1000,
                "totalGasMcf30d": 500,
                "fieldDailyProduction": [],
                "artificialLiftBreakdown": [],
                "topWellsByDecline": []
            }
            """, Encoding.UTF8, "application/json")
        }, new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "intent": 1,
                "isSupported": true,
                "message": "Top 2 wells by decline rate.",
                "data": {
                    "Wells": [
                        { "WellName": "Alpha", "DailyDeclinePercent": 0.12 },
                        { "WellName": "Beta", "DailyDeclinePercent": 0.09 }
                    ]
                }
            }
            """, Encoding.UTF8, "application/json")
        });

        Services.AddSingleton(apiClient);

        var cut = RenderComponent<Dashboard>();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Ask PetroVisor")).Click();
        cut.Find("aside input[type=text]").Input("Which wells are declining fastest?");
        cut.FindAll("aside button").Last().Click();

        Assert.Contains("Top 2 wells by decline rate.", cut.Markup);
        Assert.Contains("chat-chart-", cut.Markup);
    }

    [Fact]
    public void DashboardPage_OpensAskPetrovisorFlyoutFromTheRight()
    {
        var apiClient = CreateApiClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "wellCount": 3,
                "facilityCount": 2,
                "totalOilBbl30d": 1000,
                "totalGasMcf30d": 500,
                "fieldDailyProduction": [],
                "artificialLiftBreakdown": [],
                "topWellsByDecline": []
            }
            """, Encoding.UTF8, "application/json")
        }, new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
                "intent": 0,
                "isSupported": false,
                "message": "I can't answer that yet.",
                "data": {}
            }
            """, Encoding.UTF8, "application/json")
        });

        Services.AddSingleton(apiClient);

        var cut = RenderComponent<Dashboard>();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Ask PetroVisor")).Click();

        Assert.Contains("AI assistant", cut.Markup);
        Assert.Contains("Ask about decline, lift status, or production trends", cut.Markup);
        Assert.Contains("ask-petrovisor-drawer", cut.Markup);
        Assert.Empty(cut.FindAll("button.ask-petrovisor-toggle"));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Close").Click();

        Assert.Empty(cut.FindAll("aside.ask-petrovisor-drawer"));
        Assert.Single(cut.FindAll("button.ask-petrovisor-toggle"));
    }

    [Fact]
    public async Task PetroVisorApiClient_GetFacilityComparisonAsync_DeserializesComparisonPayload()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/api/facilities/comparison"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                    {
                      "rangeStart": "2024-01-01",
                      "rangeEnd": "2024-01-31",
                      "facilities": [
                        { "facilityId": "d1f4d7f2-2a64-4a8d-97d5-9a4f7e9d2d88", "facilityName": "Zulu", "facilityType": "Battery", "tiedInWellCount": 2, "totalOilBbl": 500, "totalGasMcf": 0, "totalWaterBbl": 0, "hasNoReportedProduction": false },
                        { "facilityId": "9f3a11aa-a2ad-4f14-bf5b-8a3d9f9613a2", "facilityName": "Alpha", "facilityType": "Battery", "tiedInWellCount": 3, "totalOilBbl": 900, "totalGasMcf": 0, "totalWaterBbl": 0, "hasNoReportedProduction": false }
                      ]
                    }
                    """, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var apiClient = new PetroVisorApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });

        var dto = await apiClient.GetFacilityComparisonAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        Assert.NotNull(dto);
        Assert.Equal(new[] { "Zulu", "Alpha" }, dto!.Facilities.Select(f => f.FacilityName));
    }

    private static PetroVisorApiClient CreateApiClient(HttpResponseMessage dashboardResponse, HttpResponseMessage assistantResponse)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/api/kpi/dashboard"))
            {
                return dashboardResponse;
            }

            if (request.Method == HttpMethod.Post && request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/api/assistant/query"))
            {
                return assistantResponse;
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
