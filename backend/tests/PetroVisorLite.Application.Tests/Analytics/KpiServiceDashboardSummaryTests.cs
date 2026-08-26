using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;
using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Application.Tests.Analytics;

public class KpiServiceDashboardSummaryTests
{
    private static Well MakeWell(string name, ArtificialLiftType liftType, Guid? facilityId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        ApiNumber = $"API-{name}",
        ArtificialLiftType = liftType,
        FacilityId = facilityId,
    };

    private static ProductionRecord Record(Guid wellId, DateOnly date, double oil, double gas = 0, double water = 0) => new()
    {
        Id = Guid.NewGuid(),
        WellId = wellId,
        Date = date,
        OilVolumeBbl = oil,
        GasVolumeMcf = gas,
        WaterVolumeBbl = water,
    };

    private class FakeWellRepository : IWellRepository
    {
        private readonly List<Well> _wells;
        public FakeWellRepository(IEnumerable<Well> wells) => _wells = wells.ToList();

        public Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_wells.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Well>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Well>>(_wells);

        public Task AddAsync(Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeProductionRecordRepository : IProductionRecordRepository
    {
        private readonly List<ProductionRecord> _records;
        public FakeProductionRecordRepository(IEnumerable<ProductionRecord> records) => _records = records.ToList();

        public Task<IReadOnlyList<ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
        {
            var query = _records.Where(r => r.WellId == wellId);
            if (from.HasValue) query = query.Where(r => r.Date >= from.Value);
            if (to.HasValue) query = query.Where(r => r.Date <= to.Value);
            return Task.FromResult<IReadOnlyList<ProductionRecord>>(query.OrderBy(r => r.Date).ToList());
        }

        public Task AddRangeAsync(IEnumerable<ProductionRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_AggregatesCountsTotalsAndCharts()
    {
        var start = new DateOnly(2026, 1, 1);
        var facilityId = Guid.NewGuid();

        var wellA = MakeWell("Well A", ArtificialLiftType.Esp, facilityId);
        var wellB = MakeWell("Well B", ArtificialLiftType.RodPump);

        var records = new List<ProductionRecord>();
        for (int t = 0; t < 10; t++)
        {
            // Well A: steady exponential decline so a decline rate can be fit.
            records.Add(Record(wellA.Id, start.AddDays(t), oil: 100 * Math.Pow(0.98, t), gas: 50, water: 5));
            // Well B: flat production, no facility assigned.
            records.Add(Record(wellB.Id, start.AddDays(t), oil: 20, gas: 10, water: 2));
        }

        var kpiService = new KpiService(
            new FakeProductionRecordRepository(records),
            new FakeWellRepository(new[] { wellA, wellB }),
            new ProductionKpiCalculator(),
            new DeclineRateCalculator());

        var summary = await kpiService.GetDashboardSummaryAsync(start, start.AddDays(9));

        Assert.Equal(2, summary.WellCount);
        Assert.Equal(1, summary.FacilityCount);
        Assert.Equal(records.Sum(r => r.OilVolumeBbl), summary.TotalOilBbl30d, precision: 6);
        Assert.Equal(records.Sum(r => r.GasVolumeMcf), summary.TotalGasMcf30d, precision: 6);

        Assert.Equal(10, summary.FieldDailyProduction.Count);
        Assert.All(summary.FieldDailyProduction, d => Assert.Equal(60, d.GasVolumeMcf, precision: 6));

        // Lift breakdown covers both lift types with correct well counts.
        Assert.Equal(2, summary.ArtificialLiftBreakdown.Count);
        Assert.Contains(summary.ArtificialLiftBreakdown, b => b.ArtificialLiftType == "Esp" && b.WellCount == 1);
        Assert.Contains(summary.ArtificialLiftBreakdown, b => b.ArtificialLiftType == "RodPump" && b.WellCount == 1);

        // Well A should be recovered with a positive decline rate; Well B is flat (no decline).
        var wellARanking = Assert.Single(summary.TopWellsByDecline, r => r.WellId == wellA.Id);
        Assert.NotNull(wellARanking.DailyDeclinePercent);
        Assert.True(wellARanking.DailyDeclinePercent > 0);
    }
}
