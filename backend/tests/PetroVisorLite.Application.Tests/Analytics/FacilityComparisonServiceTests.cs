using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Tests.Analytics;

public class FacilityComparisonServiceTests
{
    [Fact]
    public async Task GetFacilityComparisonAsync_UsesFacilitiesAsSourceOfTruth_AndSumsProductionForRange()
    {
        var alphaWell1 = new Well { Id = Guid.NewGuid(), Name = "Alpha 1", FacilityId = Guid.NewGuid() };
        var alphaWell2 = new Well { Id = Guid.NewGuid(), Name = "Alpha 2", FacilityId = alphaWell1.FacilityId };
        var bravoWell = new Well { Id = Guid.NewGuid(), Name = "Bravo 1" };
        var zuluWell = new Well { Id = Guid.NewGuid(), Name = "Zulu 1" };

        var facilities = new[]
        {
            new Facility { Id = Guid.NewGuid(), Name = "Bravo", Type = "Battery", Wells = new List<Well> { bravoWell } },
            new Facility { Id = Guid.NewGuid(), Name = "Alpha", Type = "Battery", Wells = new List<Well> { alphaWell1, alphaWell2 } },
            new Facility { Id = Guid.NewGuid(), Name = "Zulu", Type = "Battery", Wells = new List<Well> { zuluWell } }
        };

        var productionByWell = new Dictionary<Guid, IReadOnlyList<ProductionRecord>>
        {
            [alphaWell1.Id] = new[]
            {
                new ProductionRecord { Id = Guid.NewGuid(), WellId = alphaWell1.Id, Date = new DateOnly(2024, 1, 3), OilVolumeBbl = 120, GasVolumeMcf = 40, WaterVolumeBbl = 15 },
                new ProductionRecord { Id = Guid.NewGuid(), WellId = alphaWell1.Id, Date = new DateOnly(2024, 1, 20), OilVolumeBbl = 80, GasVolumeMcf = 20, WaterVolumeBbl = 5 }
            },
            [alphaWell2.Id] = new[]
            {
                new ProductionRecord { Id = Guid.NewGuid(), WellId = alphaWell2.Id, Date = new DateOnly(2024, 2, 5), OilVolumeBbl = 400, GasVolumeMcf = 100, WaterVolumeBbl = 40 }
            },
            [bravoWell.Id] = new[]
            {
                new ProductionRecord { Id = Guid.NewGuid(), WellId = bravoWell.Id, Date = new DateOnly(2024, 1, 10), OilVolumeBbl = 250, GasVolumeMcf = 80, WaterVolumeBbl = 25 },
                new ProductionRecord { Id = Guid.NewGuid(), WellId = bravoWell.Id, Date = new DateOnly(2024, 1, 12), OilVolumeBbl = 50, GasVolumeMcf = 10, WaterVolumeBbl = 5 }
            },
            [zuluWell.Id] = Array.Empty<ProductionRecord>()
        };

        var service = new FacilityComparisonService(
            new FakeFacilityRepository(facilities),
            new FakeProductionRecordRepository(productionByWell));

        var dto = await service.GetFacilityComparisonAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));

        Assert.Equal(new[] { "Bravo", "Alpha", "Zulu" }, dto.Facilities.Select(f => f.FacilityName).ToArray());
        Assert.Equal(3, dto.Facilities.Count);
        Assert.Equal(1, dto.Facilities[0].TiedInWellCount);
        Assert.Equal(300, dto.Facilities[0].TotalOilBbl);
        Assert.Equal(90, dto.Facilities[0].TotalGasMcf);
        Assert.Equal(30, dto.Facilities[0].TotalWaterBbl);
        Assert.False(dto.Facilities[0].HasNoReportedProduction);

        var alphaRow = dto.Facilities[1];
        Assert.Equal(2, alphaRow.TiedInWellCount);
        Assert.Equal(200, alphaRow.TotalOilBbl);
        Assert.Equal(60, alphaRow.TotalGasMcf);
        Assert.Equal(20, alphaRow.TotalWaterBbl);
        Assert.False(alphaRow.HasNoReportedProduction);

        var zuluRow = dto.Facilities[2];
        Assert.Equal(1, zuluRow.TiedInWellCount);
        Assert.Equal(0, zuluRow.TotalOilBbl);
        Assert.Equal(0, zuluRow.TotalGasMcf);
        Assert.Equal(0, zuluRow.TotalWaterBbl);
        Assert.True(zuluRow.HasNoReportedProduction);
    }

    private sealed class FakeFacilityRepository : IFacilityRepository
    {
        private readonly IReadOnlyList<Facility> _facilities;

        public FakeFacilityRepository(IReadOnlyList<Facility> facilities) => _facilities = facilities;

        public Task<Facility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_facilities.FirstOrDefault(f => f.Id == id));

        public Task<IReadOnlyList<Facility>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_facilities);

        public Task AddAsync(Facility facility, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Facility facility, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeProductionRecordRepository : IProductionRecordRepository
    {
        private readonly IReadOnlyDictionary<Guid, IReadOnlyList<ProductionRecord>> _recordsByWell;

        public FakeProductionRecordRepository(IReadOnlyDictionary<Guid, IReadOnlyList<ProductionRecord>> recordsByWell) =>
            _recordsByWell = recordsByWell;

        public Task<IReadOnlyList<ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
        {
            if (!_recordsByWell.TryGetValue(wellId, out var records))
            {
                return Task.FromResult<IReadOnlyList<ProductionRecord>>(Array.Empty<ProductionRecord>());
            }

            var filtered = records
                .Where(r => (!from.HasValue || r.Date >= from.Value) && (!to.HasValue || r.Date <= to.Value))
                .ToList();

            return Task.FromResult<IReadOnlyList<ProductionRecord>>(filtered);
        }

        public Task AddRangeAsync(IEnumerable<ProductionRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
