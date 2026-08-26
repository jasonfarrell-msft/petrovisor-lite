using PetroVisorLite.Web.Analytics;
using PetroVisorLite.Web.Models;

namespace PetroVisorLite.Web.Tests;

public class ProductionTrendAnalyzerTests
{
    private static ProductionRecordDto Record(int dayOffset, double oilBbl) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dayOffset),
        oilBbl,
        0,
        0,
        0,
        0,
        "ESP",
        "Running");

    [Fact]
    public void EstimateDailyDeclinePercent_ReturnsNull_WhenFewerThanFivePoints()
    {
        var records = Enumerable.Range(0, 3).Select(i => Record(i, 100)).ToList();

        var result = ProductionTrendAnalyzer.EstimateDailyDeclinePercent(records);

        Assert.Null(result);
    }

    [Fact]
    public void EstimateDailyDeclinePercent_RecoversKnownExponentialDeclineRate()
    {
        const double q0 = 1000;
        const double dailyDecline = 0.02;
        var records = Enumerable.Range(0, 60)
            .Select(i => Record(i, q0 * Math.Exp(-dailyDecline * i)))
            .ToList();

        var result = ProductionTrendAnalyzer.EstimateDailyDeclinePercent(records);

        Assert.NotNull(result);
        Assert.Equal(dailyDecline, result.Value, precision: 6);
    }

    [Fact]
    public void CountLossFlags_ReturnsZero_WhenNoDrops()
    {
        var records = Enumerable.Range(0, 20).Select(i => Record(i, 500)).ToList();

        var flagCount = ProductionTrendAnalyzer.CountLossFlags(records);

        Assert.Equal(0, flagCount);
    }

    [Fact]
    public void CountLossFlags_FlagsSignificantDrop()
    {
        var records = Enumerable.Range(0, 10).Select(i => Record(i, 500)).ToList();
        records.Add(Record(10, 100)); // > 15% below trailing 7-day average

        var flagCount = ProductionTrendAnalyzer.CountLossFlags(records);

        Assert.Equal(1, flagCount);
    }

    [Fact]
    public void EstimateDailyDeclinePercent_IgnoresNonPositiveVolumes()
    {
        var records = new List<ProductionRecordDto>
        {
            Record(0, 0),
            Record(1, 100),
            Record(2, 90),
            Record(3, 81),
            Record(4, 73),
            Record(5, 66),
        };

        var result = ProductionTrendAnalyzer.EstimateDailyDeclinePercent(records);

        Assert.NotNull(result);
        Assert.True(result > 0);
    }
}
