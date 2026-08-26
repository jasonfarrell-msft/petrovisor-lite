using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Domain;
using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Application.Tests.Analytics;

public class ArtificialLiftMonitorTests
{
    private static ProductionRecord Record(Guid wellId, DateOnly date, double oil, ArtificialLiftType liftType, ArtificialLiftStatus liftStatus) => new()
    {
        Id = Guid.NewGuid(),
        WellId = wellId,
        Date = date,
        OilVolumeBbl = oil,
        ArtificialLiftType = liftType,
        ArtificialLiftStatus = liftStatus,
    };

    [Fact]
    public void DetectLiftEvents_FlagsLiftStatusChange_CorrelatedWithProductionDrop()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        var records = new List<ProductionRecord>();
        // Stable baseline: RodPump, Running, ~100 bbl/day for 7 days.
        for (int i = 0; i < 7; i++)
        {
            records.Add(Record(wellId, start.AddDays(i), 100, ArtificialLiftType.RodPump, ArtificialLiftStatus.Running));
        }
        // Pump goes down and production craters.
        records.Add(Record(wellId, start.AddDays(7), 10, ArtificialLiftType.RodPump, ArtificialLiftStatus.Down));

        var monitor = new ArtificialLiftMonitor();
        var flags = monitor.DetectLiftEvents(records, baselineWindowDays: 7, productionDropThresholdPercent: 0.15);

        var flag = Assert.Single(flags);
        Assert.Equal(start.AddDays(7), flag.Date);
        Assert.Equal(ArtificialLiftStatus.Running, flag.PreviousLiftStatus);
        Assert.Equal(ArtificialLiftStatus.Down, flag.CurrentLiftStatus);
        Assert.True(flag.IsStatusOrTypeChange);
        Assert.True(flag.IsCorrelatedWithProductionDrop);
    }

    [Fact]
    public void DetectLiftEvents_FlagsLiftTypeChange_WithoutRequiringProductionDrop()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        var records = new List<ProductionRecord>();
        for (int i = 0; i < 7; i++)
        {
            records.Add(Record(wellId, start.AddDays(i), 100, ArtificialLiftType.RodPump, ArtificialLiftStatus.Running));
        }
        // Well converted to ESP, production holds steady (no drop) — should still be flagged as a
        // change event, just not marked as correlated with a drop.
        records.Add(Record(wellId, start.AddDays(7), 100, ArtificialLiftType.Esp, ArtificialLiftStatus.Running));

        var monitor = new ArtificialLiftMonitor();
        var flags = monitor.DetectLiftEvents(records);

        var flag = Assert.Single(flags);
        Assert.True(flag.IsStatusOrTypeChange);
        Assert.False(flag.IsCorrelatedWithProductionDrop);
    }

    [Fact]
    public void DetectLiftEvents_NoChanges_ProducesNoFlags()
    {
        var wellId = Guid.NewGuid();
        var start = new DateOnly(2026, 1, 1);

        var records = Enumerable.Range(0, 10)
            .Select(i => Record(wellId, start.AddDays(i), 100, ArtificialLiftType.Esp, ArtificialLiftStatus.Running))
            .ToList();

        var monitor = new ArtificialLiftMonitor();
        var flags = monitor.DetectLiftEvents(records);

        Assert.Empty(flags);
    }
}
