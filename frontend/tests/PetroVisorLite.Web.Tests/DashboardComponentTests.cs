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
        cut.Find("input[type=text]").Input("Can you explain the weather?");
        cut.FindAll("button").Last().Click();

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
        cut.Find("input[type=text]").Input("Which wells are declining fastest?");
        cut.FindAll("button").Last().Click();

        Assert.Contains("Top 2 wells by decline rate.", cut.Markup);
        Assert.Contains("chat-chart-", cut.Markup);
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
