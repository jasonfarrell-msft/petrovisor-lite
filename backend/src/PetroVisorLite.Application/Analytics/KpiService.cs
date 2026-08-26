using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Application.Analytics;

/// <summary>
/// Default <see cref="IKpiService"/> implementation. Thin orchestration layer: pulls a well's
/// production history from <see cref="IProductionRecordRepository"/> and delegates the actual
/// number-crunching to the focused, framework-agnostic calculators below (each of which operates
/// purely on in-memory <c>IEnumerable&lt;ProductionRecord&gt;</c> and is independently unit-testable).
///
/// This is the seam where a future ML.NET-based implementation would slot in: either swap out
/// <see cref="IDeclineRateCalculator"/>/<see cref="IProductionLossDetector"/> for ML.NET-backed
/// implementations, or introduce a new <c>IKpiService</c> implementation that calls into an
/// ML.NET <c>PredictionEngine</c> using the same repository data as features.
/// </summary>
public class KpiService : IKpiService
{
    private readonly IProductionRecordRepository _productionRecordRepository;
    private readonly IWellRepository _wellRepository;
    private readonly IProductionKpiCalculator _productionKpiCalculator;
    private readonly IDeclineRateCalculator _declineRateCalculator;

    public KpiService(
        IProductionRecordRepository productionRecordRepository,
        IWellRepository wellRepository,
        IProductionKpiCalculator productionKpiCalculator,
        IDeclineRateCalculator declineRateCalculator)
    {
        _productionRecordRepository = productionRecordRepository;
        _wellRepository = wellRepository;
        _productionKpiCalculator = productionKpiCalculator;
        _declineRateCalculator = declineRateCalculator;
    }

    public async Task<ProductionKpiDto> GetProductionKpiAsync(Guid wellId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        var records = await _productionRecordRepository.GetByWellIdAsync(wellId, periodStart, periodEnd, cancellationToken);

        var dailyTotals = _productionKpiCalculator.CalculateDailyTotals(records);

        double totalOil = dailyTotals.Sum(t => t.OilVolumeBbl);
        double totalGas = dailyTotals.Sum(t => t.GasVolumeMcf);
        double averageDailyOil = dailyTotals.Count > 0 ? totalOil / dailyTotals.Count : 0;

        return new ProductionKpiDto(wellId, periodStart, periodEnd, totalOil, totalGas, averageDailyOil);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        var wells = await _wellRepository.GetAllAsync(cancellationToken);

        // Pull each well's production history for the period once, then derive every chart/card
        // from that in-memory data — a single pass per well instead of one HTTP round trip per well.
        var wellHistories = new List<(PetroVisorLite.Domain.Well Well, IReadOnlyList<PetroVisorLite.Domain.ProductionRecord> Records)>();
        foreach (var well in wells)
        {
            var records = await _productionRecordRepository.GetByWellIdAsync(well.Id, periodStart, periodEnd, cancellationToken);
            wellHistories.Add((well, records));
        }

        var facilityCount = wells.Select(w => w.FacilityId).Where(id => id.HasValue).Distinct().Count();

        var allRecords = wellHistories.SelectMany(h => h.Records).ToList();

        double totalOil = allRecords.Sum(r => r.OilVolumeBbl);
        double totalGas = allRecords.Sum(r => r.GasVolumeMcf);

        // Field-wide daily production trend: sum oil/gas/water across all wells per calendar day.
        var fieldDaily = allRecords
            .GroupBy(r => r.Date)
            .Select(g => new FieldDailyProductionDto(g.Key, g.Sum(r => r.OilVolumeBbl), g.Sum(r => r.GasVolumeMcf), g.Sum(r => r.WaterVolumeBbl)))
            .OrderBy(d => d.Date)
            .ToList();

        // Production by artificial lift type: well count + total oil per lift type (well's current
        // configured lift type, not the per-record value, so it reflects current operational mix).
        var liftBreakdown = wellHistories
            .GroupBy(h => h.Well.ArtificialLiftType.ToString())
            .Select(g => new ArtificialLiftBreakdownDto(g.Key, g.Count(), g.Sum(h => h.Records.Sum(r => r.OilVolumeBbl))))
            .OrderByDescending(b => b.TotalOilBbl30d)
            .ToList();

        // Top wells by decline rate: fit each well's decline curve and rank by daily decline percent.
        var declineRankings = wellHistories
            .Select(h => (h.Well, Decline: _declineRateCalculator.CalculateDeclineRate(h.Records)))
            .Select(x => new WellDeclineRankingDto(x.Well.Id, x.Well.Name, x.Well.ApiNumber, x.Decline?.DailyDeclinePercent, x.Decline?.AnnualDeclinePercent))
            .OrderByDescending(r => r.DailyDeclinePercent ?? double.MinValue)
            .Take(10)
            .ToList();

        return new DashboardSummaryDto(
            wells.Count,
            facilityCount,
            totalOil,
            totalGas,
            fieldDaily,
            liftBreakdown,
            declineRankings);
    }
}
