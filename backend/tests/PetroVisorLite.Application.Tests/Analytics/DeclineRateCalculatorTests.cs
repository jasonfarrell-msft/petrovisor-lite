using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Tests.Analytics;

public class DeclineRateCalculatorTests
{
    private static ProductionRecord Record(Guid wellId, DateOnly date, double oil) => new()
    {
        Id = Guid.NewGuid(),
        WellId = wellId,
        Date = date,
        OilVolumeBbl = oil,
    };

    [Fact]
    public void CalculateDeclineRate_OnSyntheticExponentialDecline_RecoversApproximateRate()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);
        const double q0 = 1000;
        const double dailyDeclineTrue = 0.01; // 1%/day

        // Perfect exponential decline series: q(t) = q0 * (1 - d)^t
        var records = Enumerable.Range(0, 60)
            .Select(t => Record(wellId, start.AddDays(t), q0 * Math.Pow(1 - dailyDeclineTrue, t)))
            .ToList();

        var calculator = new DeclineRateCalculator();
        var result = calculator.CalculateDeclineRate(records);

        Assert.NotNull(result);
        Assert.Equal(wellId, result!.WellId);
        Assert.Equal(60, result.PointsUsed);
        // Fitted daily decline should closely match the true synthetic rate.
        Assert.InRange(result.DailyDeclinePercent, dailyDeclineTrue - 0.0005, dailyDeclineTrue + 0.0005);
        Assert.True(result.AnnualDeclinePercent > 0);
    }

    [Fact]
    public void CalculateDeclineRate_WithIncreasingProduction_ReturnsNegativeDeclineRate()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        var records = Enumerable.Range(0, 20)
            .Select(t => Record(wellId, start.AddDays(t), 100 + t * 10)) // steadily increasing
            .ToList();

        var calculator = new DeclineRateCalculator();
        var result = calculator.CalculateDeclineRate(records);

        Assert.NotNull(result);
        Assert.True(result!.DailyDeclinePercent < 0); // negative "decline" == growth
    }

    [Fact]
    public void CalculateDeclineRate_WithFewerThanMinimumPoints_ReturnsNull()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        var records = new[]
        {
            Record(wellId, start, 100),
            Record(wellId, start.AddDays(1), 95),
        };

        var calculator = new DeclineRateCalculator();
        var result = calculator.CalculateDeclineRate(records, minimumPoints: 5);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateDeclineRate_ExcludesZeroVolumeShutInDays()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);
        const double q0 = 500;
        const double dailyDeclineTrue = 0.02;

        var records = Enumerable.Range(0, 30)
            .Select(t => Record(wellId, start.AddDays(t), q0 * Math.Pow(1 - dailyDeclineTrue, t)))
            .ToList();

        // Inject shut-in (zero volume) days that would corrupt a naive log fit if not excluded.
        records.Add(Record(wellId, start.AddDays(30), 0));
        records.Add(Record(wellId, start.AddDays(31), 0));

        var calculator = new DeclineRateCalculator();
        var result = calculator.CalculateDeclineRate(records);

        Assert.NotNull(result);
        Assert.Equal(30, result!.PointsUsed); // the 2 zero-volume days should be excluded
        Assert.InRange(result.DailyDeclinePercent, dailyDeclineTrue - 0.001, dailyDeclineTrue + 0.001);
    }
}
