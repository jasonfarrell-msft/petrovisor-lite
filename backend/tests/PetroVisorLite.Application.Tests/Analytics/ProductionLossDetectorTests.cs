using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Tests.Analytics;

public class ProductionLossDetectorTests
{
    private static ProductionRecord Record(Guid wellId, DateOnly date, double oil) => new()
    {
        Id = Guid.NewGuid(),
        WellId = wellId,
        Date = date,
        OilVolumeBbl = oil,
    };

    [Fact]
    public void DetectLosses_FlagsDayWithInjectedFiftyPercentDrop()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        // Steady baseline of 100 bbl/day for 10 days, then a day at 50 bbl (50% below expected).
        var records = Enumerable.Range(0, 10)
            .Select(t => Record(wellId, start.AddDays(t), 100))
            .ToList();
        var anomalyDate = start.AddDays(10);
        records.Add(Record(wellId, anomalyDate, 50));
        // Continue steady production afterward.
        records.AddRange(Enumerable.Range(11, 5).Select(t => Record(wellId, start.AddDays(t), 100)));

        var detector = new ProductionLossDetector();
        var flags = detector.DetectLosses(records, windowSizeDays: 7, thresholdPercent: 0.15);

        var flag = Assert.Single(flags);
        Assert.Equal(anomalyDate, flag.Date);
        Assert.Equal(50, flag.ActualOilVolumeBbl);
        Assert.Equal(100, flag.ExpectedOilVolumeBbl);
        Assert.InRange(flag.PercentBelowExpected, 0.49, 0.51);
    }

    [Fact]
    public void DetectLosses_DoesNotFlagNormalVariation()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        // Small day-to-day noise (+/- 5%) around 100 bbl/day baseline — should stay under the
        // default 15% threshold and not be flagged.
        double[] volumes = { 100, 102, 98, 101, 99, 103, 97, 100, 104, 96, 100, 98, 102, 99 };
        var records = volumes
            .Select((v, i) => Record(wellId, start.AddDays(i), v))
            .ToList();

        var detector = new ProductionLossDetector();
        var flags = detector.DetectLosses(records, windowSizeDays: 7, thresholdPercent: 0.15);

        Assert.Empty(flags);
    }

    [Fact]
    public void DetectLosses_RequiresFullWarmupWindowBeforeFlagging()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        // Only 3 days of history with a 7-day window configured — no day has enough trailing
        // history, so nothing should be evaluated/flagged regardless of volume.
        var records = new[]
        {
            Record(wellId, start, 100),
            Record(wellId, start.AddDays(1), 100),
            Record(wellId, start.AddDays(2), 10),
        };

        var detector = new ProductionLossDetector();
        var flags = detector.DetectLosses(records, windowSizeDays: 7, thresholdPercent: 0.15);

        Assert.Empty(flags);
    }
}
