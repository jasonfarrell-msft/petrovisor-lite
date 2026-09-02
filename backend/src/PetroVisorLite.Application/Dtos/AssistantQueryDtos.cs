namespace PetroVisorLite.Application.Dtos;

public enum QueryIntent
{
    Unsupported = 0,
    TopWellsByDeclineRate = 1,
    WellsByArtificialLiftStatus = 2,
    FieldProductionTrendSummary = 3,
}

public record AssistantQueryRequest(string Question);

public record QueryIntentParameters(
    int TopN = 5,
    string? ArtificialLiftStatus = null,
    DateOnly? PeriodStart = null,
    DateOnly? PeriodEnd = null);

public record QueryIntentClassification(
    QueryIntent Intent,
    QueryIntentParameters Parameters,
    bool IsSupported,
    string Reason)
{
    public static QueryIntentClassification Unsupported(string reason) =>
        new(QueryIntent.Unsupported, new QueryIntentParameters(), false, reason);
}

public record TopWellsByDeclineResponseDto(int RequestedLimit, IReadOnlyList<WellDeclineRankingDto> Wells);

public record ArtificialLiftStatusBreakdownDto(string ArtificialLiftStatus, int WellCount);

public record WellsByArtificialLiftStatusResponseDto(IReadOnlyList<ArtificialLiftStatusBreakdownDto> Breakdowns);

public record AssistantQueryResponse(
    QueryIntent Intent,
    bool IsSupported,
    string Message,
    object? Data = null);
