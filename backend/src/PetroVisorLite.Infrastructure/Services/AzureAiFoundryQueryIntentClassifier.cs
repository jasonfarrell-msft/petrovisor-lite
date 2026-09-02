using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Infrastructure.Services;

public sealed class AzureAiFoundryOptions
{
    public const string SectionName = "AzureAiFoundry";

    public string Endpoint { get; set; } = string.Empty;
    public string? DeploymentName { get; set; }
    public string? ModelName { get; set; }
}

/// <summary>
/// Constrained classifier for a small allow-list of approved query intents. When Azure AI Foundry is
/// not configured, the classifier falls back to a deterministic keyword match so unsupported questions
/// are rejected safely instead of producing fabricated values.
/// </summary>
public class AzureAiFoundryQueryIntentClassifier : IQueryIntentClassifier
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<AzureAiFoundryOptions> _options;
    private readonly TokenCredential _credential;
    private readonly ILogger<AzureAiFoundryQueryIntentClassifier> _logger;

    public AzureAiFoundryQueryIntentClassifier(
        HttpClient httpClient,
        IOptions<AzureAiFoundryOptions> options,
        TokenCredential credential,
        ILogger<AzureAiFoundryQueryIntentClassifier> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _credential = credential;
        _logger = logger;
    }

    public async Task<QueryIntentClassification> ClassifyAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            return QueryIntentClassification.Unsupported("I can't answer that yet.");
        }

        var config = _options.Value;
        if (!string.IsNullOrWhiteSpace(config.Endpoint) &&
            (!string.IsNullOrWhiteSpace(config.DeploymentName) || !string.IsNullOrWhiteSpace(config.ModelName)))
        {
            try
            {
                var foundryResult = await TryClassifyWithFoundryAsync(userQuestion, config, cancellationToken);
                if (foundryResult is not null)
                {
                    _logger.LogInformation(
                        "Microsoft Foundry selected intent {Intent}.",
                        foundryResult.Intent);
                    return foundryResult;
                }
            }
            catch (AuthenticationFailedException exception)
            {
                _logger.LogWarning(exception, "Managed identity authentication to Microsoft Foundry failed; using local intent classification.");
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Microsoft Foundry request failed; using local intent classification.");
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Microsoft Foundry returned an invalid intent response; using local intent classification.");
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Microsoft Foundry request timed out; using local intent classification.");
            }
        }

        return ClassifyLocally(userQuestion);
    }

    private async Task<QueryIntentClassification?> TryClassifyWithFoundryAsync(string userQuestion, AzureAiFoundryOptions config, CancellationToken cancellationToken)
    {
        var modelName = !string.IsNullOrWhiteSpace(config.DeploymentName)
            ? config.DeploymentName
            : config.ModelName;

        if (string.IsNullOrWhiteSpace(modelName))
        {
            return null;
        }

        var requestUri = new Uri($"{config.Endpoint.TrimEnd('/')}/openai/v1/chat/completions");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new
            {
                model = modelName,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a strict router for PetroVisor Lite. Reply with a JSON object only. Allowed intent values are Unsupported, TopWellsByDeclineRate, WellsByArtificialLiftStatus, FieldProductionTrendSummary. Do not invent any other intent. If the question is outside the approved set, return {\"intent\":\"Unsupported\"}. Include topN for decline queries when the user asks for top N wells, otherwise omit it."
                    },
                    new
                    {
                        role = "user",
                        content = userQuestion
                    }
                },
                response_format = new { type = "json_object" }
            })
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var accessToken = await _credential.GetTokenAsync(
            new TokenRequestContext(["https://ai.azure.com/.default"]),
            cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Microsoft Foundry intent classification returned HTTP {StatusCode}; using local intent classification.",
                (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var intentValue = GetIntent(root);
        if (!string.IsNullOrWhiteSpace(intentValue))
        {
            return MapIntent(intentValue, root);
        }

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    var content = contentElement.GetString();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return MapIntentFromContent(content);
                    }
                }
            }
        }

        return QueryIntentClassification.Unsupported("I can't answer that yet.");
    }

    private static QueryIntentClassification ClassifyLocally(string userQuestion)
    {
        var normalized = userQuestion.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return QueryIntentClassification.Unsupported("I can't answer that yet.");
        }

        var question = normalized.ToLowerInvariant();

        if (ContainsAny(question, "decline", "drop", "top") && ContainsAny(question, "well", "wells"))
        {
            var topN = ParseTopN(normalized);
            return new QueryIntentClassification(
                QueryIntent.TopWellsByDeclineRate,
                new QueryIntentParameters(TopN: topN),
                true,
                "Decline-rate intent matched approved query set.");
        }

        if (ContainsAny(question, "artificial lift", "lift status", "lift type", "esp", "rod pump", "gas lift", "pcp"))
        {
            return new QueryIntentClassification(
                QueryIntent.WellsByArtificialLiftStatus,
                new QueryIntentParameters(),
                true,
                "Artificial lift intent matched approved query set.");
        }

        if (ContainsAny(question, "production trend", "field production", "field summary", "overall production", "production summary"))
        {
            return new QueryIntentClassification(
                QueryIntent.FieldProductionTrendSummary,
                new QueryIntentParameters(),
                true,
                "Field production summary intent matched approved query set.");
        }

        return QueryIntentClassification.Unsupported("I can't answer that yet.");
    }

    private static QueryIntentClassification MapIntent(string intentValue, JsonElement root)
    {
        var normalized = intentValue.Trim();
        var candidate = normalized.Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

        var parameters = new QueryIntentParameters(
            TopN: TryReadInt(root, "topN", 5),
            ArtificialLiftStatus: TryReadString(root, "artificialLiftStatus"),
            PeriodStart: TryReadDate(root, "periodStart"),
            PeriodEnd: TryReadDate(root, "periodEnd"));

        return candidate switch
        {
            "topwellsbydeclinerate" or "topdeclinewells" or "declinerate" => new QueryIntentClassification(QueryIntent.TopWellsByDeclineRate, parameters, true, "Approved intent selected by Azure AI Foundry."),
            "wellsbyartificialliftstatus" or "artificialliftstatus" or "liftstatus" => new QueryIntentClassification(QueryIntent.WellsByArtificialLiftStatus, parameters, true, "Approved intent selected by Azure AI Foundry."),
            "fieldproductiontrendsummary" or "fieldproduction" or "productiontrendsummary" => new QueryIntentClassification(QueryIntent.FieldProductionTrendSummary, parameters, true, "Approved intent selected by Azure AI Foundry."),
            "unsupported" or "cantanswertyet" or "cannotanswer" => QueryIntentClassification.Unsupported("I can't answer that yet."),
            _ => QueryIntentClassification.Unsupported("I can't answer that yet.")
        };
    }

    private static QueryIntentClassification MapIntentFromContent(string content)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        var intentValue = GetIntent(root);
        return string.IsNullOrWhiteSpace(intentValue)
            ? QueryIntentClassification.Unsupported("I can't answer that yet.")
            : MapIntent(intentValue, root);
    }

    private static string? GetIntent(JsonElement root) =>
        GetString(root, "intent") ??
        GetString(root, "queryIntent") ??
        GetString(root, "classification");

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool ContainsAny(string input, params string[] values) => values.Any(input.Contains);

    private static int ParseTopN(string question)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            question,
            "top\\s*(\\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var topN) && topN > 0)
        {
            return topN;
        }

        return 5;
    }

    private static DateOnly? TryReadDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateOnly.TryParse(property.GetString(), out var date) ? date : null;
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static int TryReadInt(JsonElement root, string propertyName, int defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value) && value > 0
            ? value
            : defaultValue;
    }
}
