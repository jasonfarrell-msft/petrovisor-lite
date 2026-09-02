using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Application.Assistant;

/// <summary>
/// Application-layer orchestration for the approved Ask PetroVisor intents. The service stays pure:
/// it never calls an LLM and only composes existing repository/service abstractions.
/// </summary>
public class QueryOrchestrator : IQueryOrchestrator
{
    private readonly IKpiService _kpiService;
    private readonly IWellRepository _wellRepository;

    public QueryOrchestrator(IKpiService kpiService, IWellRepository wellRepository)
    {
        _kpiService = kpiService;
        _wellRepository = wellRepository;
    }

    public async Task<AssistantQueryResponse> ExecuteAsync(QueryIntentClassification classification, CancellationToken cancellationToken = default)
    {
        if (!classification.IsSupported || classification.Intent == QueryIntent.Unsupported)
        {
            return new AssistantQueryResponse(
                QueryIntent.Unsupported,
                false,
                "I can't answer that yet.");
        }

        return classification.Intent switch
        {
            QueryIntent.TopWellsByDeclineRate => await ExecuteTopWellsByDeclineAsync(classification.Parameters, cancellationToken),
            QueryIntent.WellsByArtificialLiftStatus => await ExecuteWellsByArtificialLiftStatusAsync(cancellationToken),
            QueryIntent.FieldProductionTrendSummary => await ExecuteFieldProductionTrendSummaryAsync(classification.Parameters, cancellationToken),
            _ => new AssistantQueryResponse(QueryIntent.Unsupported, false, "I can't answer that yet.")
        };
    }

    private async Task<AssistantQueryResponse> ExecuteTopWellsByDeclineAsync(QueryIntentParameters parameters, CancellationToken cancellationToken)
    {
        var periodEnd = parameters.PeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = parameters.PeriodStart ?? periodEnd.AddDays(-30);

        var summary = await _kpiService.GetDashboardSummaryAsync(periodStart, periodEnd, cancellationToken);
        var topN = Math.Max(1, parameters.TopN > 0 ? parameters.TopN : 5);
        var wells = summary.TopWellsByDecline
            .OrderByDescending(w => w.DailyDeclinePercent ?? double.MinValue)
            .Take(topN)
            .ToList();

        return new AssistantQueryResponse(
            QueryIntent.TopWellsByDeclineRate,
            true,
            $"Top {wells.Count} wells by decline rate.",
            new TopWellsByDeclineResponseDto(topN, wells));
    }

    private async Task<AssistantQueryResponse> ExecuteWellsByArtificialLiftStatusAsync(CancellationToken cancellationToken)
    {
        var wells = await _wellRepository.GetAllAsync(cancellationToken);
        var breakdowns = wells
            .GroupBy(w => w.ArtificialLiftStatus.ToString())
            .Select(g => new ArtificialLiftStatusBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(d => d.WellCount)
            .ToList();

        return new AssistantQueryResponse(
            QueryIntent.WellsByArtificialLiftStatus,
            true,
            "Wells grouped by artificial lift status.",
            new WellsByArtificialLiftStatusResponseDto(breakdowns));
    }

    private async Task<AssistantQueryResponse> ExecuteFieldProductionTrendSummaryAsync(QueryIntentParameters parameters, CancellationToken cancellationToken)
    {
        var periodEnd = parameters.PeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = parameters.PeriodStart ?? periodEnd.AddDays(-30);

        var summary = await _kpiService.GetDashboardSummaryAsync(periodStart, periodEnd, cancellationToken);
        return new AssistantQueryResponse(
            QueryIntent.FieldProductionTrendSummary,
            true,
            "Field production trend summary.",
            summary);
    }
}
