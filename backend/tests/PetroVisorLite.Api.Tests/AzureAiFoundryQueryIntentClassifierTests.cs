using System.Net;
using System.Text;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Infrastructure.Services;

namespace PetroVisorLite.Api.Tests;

public class AzureAiFoundryQueryIntentClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_UsesV1EndpointAndManagedIdentity()
    {
        var handler = new RecordingHandler(
            """{"choices":[{"message":{"content":"{\"intent\":\"TopWellsByDeclineRate\",\"topN\":10}"}}]}""");
        var credential = new StubTokenCredential();
        var classifier = CreateClassifier(handler, credential);

        var result = await classifier.ClassifyAsync("Which wells are declining fastest?");

        Assert.Equal(QueryIntent.TopWellsByDeclineRate, result.Intent);
        Assert.True(result.IsSupported);
        Assert.Equal(10, result.Parameters.TopN);
        Assert.Equal(
            "https://foundry.example/openai/v1/chat/completions",
            handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("test-token", handler.AuthorizationParameter);
        Assert.Equal(1, credential.RequestCount);
        Assert.Equal("https://ai.azure.com/.default", credential.RequestedScopes.Single());
    }

    [Fact]
    public async Task ClassifyAsync_WhenFoundryTimesOut_UsesLocalClassifier()
    {
        var credential = new StubTokenCredential();
        var classifier = new AzureAiFoundryQueryIntentClassifier(
            new HttpClient(new TimeoutHandler()),
            Options.Create(new AzureAiFoundryOptions
            {
                Endpoint = "https://foundry.example/",
                DeploymentName = "gpt-5.4"
            }),
            credential,
            NullLogger<AzureAiFoundryQueryIntentClassifier>.Instance);

        var result = await classifier.ClassifyAsync("Show artificial lift status");

        Assert.Equal(QueryIntent.WellsByArtificialLiftStatus, result.Intent);
        Assert.True(result.IsSupported);
    }

    [Fact]
    public async Task ClassifyAsync_WhenFoundryIsNotConfigured_UsesLocalClassifier()
    {
        var handler = new RecordingHandler("{}");
        var credential = new StubTokenCredential();
        var classifier = CreateClassifier(handler, credential, endpoint: string.Empty);

        var result = await classifier.ClassifyAsync("Show artificial lift status");

        Assert.Equal(QueryIntent.WellsByArtificialLiftStatus, result.Intent);
        Assert.True(result.IsSupported);
        Assert.Equal(0, handler.RequestCount);
        Assert.Equal(0, credential.RequestCount);
    }

    private static AzureAiFoundryQueryIntentClassifier CreateClassifier(
        RecordingHandler handler,
        StubTokenCredential credential,
        string endpoint = "https://foundry.example/")
    {
        var options = Options.Create(new AzureAiFoundryOptions
        {
            Endpoint = endpoint,
            DeploymentName = "gpt-5.4",
            ModelName = "gpt-5.4"
        });

        return new AzureAiFoundryQueryIntentClassifier(
            new HttpClient(handler),
            options,
            credential,
            NullLogger<AzureAiFoundryQueryIntentClassifier>.Instance);
    }

    private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new TaskCanceledException("Foundry request timed out.");
    }

    private sealed class StubTokenCredential : TokenCredential
    {
        public int RequestCount { get; private set; }
        public IReadOnlyList<string> RequestedScopes { get; private set; } = [];

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            RecordRequest(requestContext);
            return CreateToken();
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            RecordRequest(requestContext);
            return ValueTask.FromResult(CreateToken());
        }

        private void RecordRequest(TokenRequestContext requestContext)
        {
            RequestCount++;
            RequestedScopes = requestContext.Scopes;
        }

        private static AccessToken CreateToken() =>
            new("test-token", DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
