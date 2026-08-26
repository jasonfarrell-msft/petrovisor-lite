using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Tests.Analytics;

public class ProductionKpiCalculatorTests
{
    private static ProductionRecord Record(Guid wellId, DateOnly date, double oil, double gas, double water) => new()
    {
        Id = Guid.NewGuid(),
        WellId = wellId,
        Date = date,
        OilVolumeBbl = oil,
        GasVolumeMcf = gas,
        WaterVolumeBbl = water,
    };

    [Fact]
    public void CalculateDailyTotals_SumsVolumesPerWellPerDay()
    {
        var wellId = Guid.NewGuid();
        var day = new DateOnly(2026, 1, 1);

        // Two readings for the same well/day should be summed together.
        var records = new[]
        {
            Record(wellId, day, oil: 100, gas: 200, water: 10),
            Record(wellId, day, oil: 50, gas: 25, water: 5),
        };

        var calculator = new ProductionKpiCalculator();
        var totals = calculator.CalculateDailyTotals(records);

        var total = Assert.Single(totals);
        Assert.Equal(wellId, total.WellId);
        Assert.Equal(day, total.Date);
        Assert.Equal(150, total.OilVolumeBbl);
        Assert.Equal(225, total.GasVolumeMcf);
        Assert.Equal(15, total.WaterVolumeBbl);
    }

    [Fact]
    public void CalculateMonthlyTotals_SumsVolumesPerWellPerMonth()
    {
        var wellId = Guid.NewGuid();

        var records = new[]
        {
            Record(wellId, new DateOnly(2026, 1, 1), oil: 100, gas: 10, water: 1),
            Record(wellId, new DateOnly(2026, 1, 15), oil: 200, gas: 20, water: 2),
            Record(wellId, new DateOnly(2026, 1, 31), oil: 300, gas: 30, water: 3),
            Record(wellId, new DateOnly(2026, 2, 1), oil: 400, gas: 40, water: 4),
        };

        var calculator = new ProductionKpiCalculator();
        var totals = calculator.CalculateMonthlyTotals(records);

        Assert.Equal(2, totals.Count);

        var jan = totals.Single(t => t.Month == 1);
        Assert.Equal(2026, jan.Year);
        Assert.Equal(600, jan.OilVolumeBbl);
        Assert.Equal(60, jan.GasVolumeMcf);
        Assert.Equal(6, jan.WaterVolumeBbl);

        var feb = totals.Single(t => t.Month == 2);
        Assert.Equal(400, feb.OilVolumeBbl);
    }

    [Fact]
    public void CalculateDailyTotals_SeparatesDifferentWells()
    {
        var wellA = Guid.NewGuid();
        var wellB = Guid.NewGuid();
        var day = new DateOnly(2026, 3, 1);

        var records = new[]
        {
            Record(wellA, day, oil: 10, gas: 1, water: 1),
            Record(wellB, day, oil: 20, gas: 2, water: 2),
        };

        var calculator = new ProductionKpiCalculator();
        var totals = calculator.CalculateDailyTotals(records);

        Assert.Equal(2, totals.Count);
        Assert.Equal(10, totals.Single(t => t.WellId == wellA).OilVolumeBbl);
        Assert.Equal(20, totals.Single(t => t.WellId == wellB).OilVolumeBbl);
    }
}
