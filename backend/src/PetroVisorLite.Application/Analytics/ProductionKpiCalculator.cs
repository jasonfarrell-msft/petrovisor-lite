using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <inheritdoc cref="IProductionKpiCalculator"/>
public class ProductionKpiCalculator : IProductionKpiCalculator
{
    public IReadOnlyList<DailyProductionTotal> CalculateDailyTotals(IEnumerable<ProductionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return records
            .GroupBy(r => (r.WellId, r.Date))
            .Select(g => new DailyProductionTotal(
                g.Key.WellId,
                g.Key.Date,
                g.Sum(r => r.OilVolumeBbl),
                g.Sum(r => r.GasVolumeMcf),
                g.Sum(r => r.WaterVolumeBbl)))
            .OrderBy(t => t.WellId)
            .ThenBy(t => t.Date)
            .ToList();
    }

    public IReadOnlyList<MonthlyProductionTotal> CalculateMonthlyTotals(IEnumerable<ProductionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return records
            .GroupBy(r => (r.WellId, r.Date.Year, r.Date.Month))
            .Select(g => new MonthlyProductionTotal(
                g.Key.WellId,
                g.Key.Year,
                g.Key.Month,
                g.Sum(r => r.OilVolumeBbl),
                g.Sum(r => r.GasVolumeMcf),
                g.Sum(r => r.WaterVolumeBbl)))
            .OrderBy(t => t.WellId)
            .ThenBy(t => t.Year)
            .ThenBy(t => t.Month)
            .ToList();
    }
}
