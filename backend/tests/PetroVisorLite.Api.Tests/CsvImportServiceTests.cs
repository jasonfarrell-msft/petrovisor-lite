using System.Text;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;
using PetroVisorLite.Infrastructure.Services;

namespace PetroVisorLite.Api.Tests;

/// <summary>Tests for <see cref="CsvImportService"/> parsing/validation logic.</summary>
public class CsvImportServiceTests
{
    private sealed class FakeWellRepository : IWellRepository
    {
        public List<Well> Wells { get; } = new();

        public Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Wells.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Well>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Well>>(Wells);

        public Task AddAsync(Well well, CancellationToken cancellationToken = default)
        {
            Wells.Add(well);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeProductionRecordRepository : IProductionRecordRepository
    {
        public List<ProductionRecord> Records { get; } = new();

        public Task<IReadOnlyList<ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductionRecord>>(Records.Where(r => r.WellId == wellId).ToList());

        public Task AddRangeAsync(IEnumerable<ProductionRecord> records, CancellationToken cancellationToken = default)
        {
            Records.AddRange(records);
            return Task.CompletedTask;
        }
    }

    private static (CsvImportService service, FakeWellRepository wells, FakeProductionRecordRepository records) CreateService(params Guid[] knownWellIds)
    {
        var wellRepo = new FakeWellRepository();
        foreach (var id in knownWellIds)
        {
            wellRepo.Wells.Add(new Well { Id = id, Name = "Test Well", ApiNumber = id.ToString() });
        }
        var recordRepo = new FakeProductionRecordRepository();
        var service = new CsvImportService(recordRepo, wellRepo);
        return (service, wellRepo, recordRepo);
    }

    private static Stream ToStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task ImportProductionRecordsAsync_ValidRows_ImportsAllAndPersists()
    {
        var wellId = Guid.NewGuid();
        var (service, _, records) = CreateService(wellId);
        var csv =
            "WellId,Date,OilVolumeBbl,GasVolumeMcf,WaterVolumeBbl,ChokeSize64th,WellheadPressurePsi,ArtificialLiftType,ArtificialLiftStatus\n" +
            $"{wellId},2026-01-01,100.5,400.2,20.1,24,250,RodPump,Running\n" +
            $"{wellId},2026-01-02,98.1,395.0,21.0,24,248,RodPump,Running\n";

        var result = await service.ImportProductionRecordsAsync(ToStream(csv));

        Assert.Equal(2, result.RowsImported);
        Assert.Equal(0, result.RowsFailed);
        Assert.Empty(result.Errors);
        Assert.Equal(2, records.Records.Count);
        Assert.Equal(100.5, records.Records[0].OilVolumeBbl);
    }

    [Fact]
    public async Task ImportProductionRecordsAsync_UnknownWellId_FailsThatRow()
    {
        var (service, _, records) = CreateService(); // no known wells
        var csv =
            "WellId,Date,OilVolumeBbl,GasVolumeMcf,WaterVolumeBbl,ChokeSize64th,WellheadPressurePsi,ArtificialLiftType,ArtificialLiftStatus\n" +
            $"{Guid.NewGuid()},2026-01-01,100.5,400.2,20.1,24,250,RodPump,Running\n";

        var result = await service.ImportProductionRecordsAsync(ToStream(csv));

        Assert.Equal(0, result.RowsImported);
        Assert.Equal(1, result.RowsFailed);
        Assert.Contains("Unknown WellId", result.Errors[0].Reason);
        Assert.Empty(records.Records);
    }

    [Fact]
    public async Task ImportProductionRecordsAsync_InvalidNumericField_FailsThatRowWithReason()
    {
        var wellId = Guid.NewGuid();
        var (service, _, _) = CreateService(wellId);
        var csv =
            "WellId,Date,OilVolumeBbl,GasVolumeMcf,WaterVolumeBbl,ChokeSize64th,WellheadPressurePsi,ArtificialLiftType,ArtificialLiftStatus\n" +
            $"{wellId},2026-01-01,NOT_A_NUMBER,400.2,20.1,24,250,RodPump,Running\n";

        var result = await service.ImportProductionRecordsAsync(ToStream(csv));

        Assert.Equal(0, result.RowsImported);
        Assert.Equal(1, result.RowsFailed);
        Assert.Contains("Invalid OilVolumeBbl", result.Errors[0].Reason);
    }

    [Fact]
    public async Task ImportProductionRecordsAsync_MixedValidAndInvalidRows_ImportsValidOnesAndReportsFailures()
    {
        var wellId = Guid.NewGuid();
        var (service, _, records) = CreateService(wellId);
        var csv =
            "WellId,Date,OilVolumeBbl,GasVolumeMcf,WaterVolumeBbl,ChokeSize64th,WellheadPressurePsi,ArtificialLiftType,ArtificialLiftStatus\n" +
            $"{wellId},2026-01-01,100.5,400.2,20.1,24,250,RodPump,Running\n" +
            $"invalid-guid,2026-01-02,98.1,395.0,21.0,24,248,RodPump,Running\n" +
            $"{wellId},not-a-date,90.0,380.0,19.0,24,245,RodPump,Running\n";

        var result = await service.ImportProductionRecordsAsync(ToStream(csv));

        Assert.Equal(1, result.RowsImported);
        Assert.Equal(2, result.RowsFailed);
        Assert.Single(records.Records);
    }

    [Fact]
    public async Task ImportProductionRecordsAsync_UnknownLiftEnum_DefaultsWithoutFailingRow()
    {
        var wellId = Guid.NewGuid();
        var (service, _, records) = CreateService(wellId);
        var csv =
            "WellId,Date,OilVolumeBbl,GasVolumeMcf,WaterVolumeBbl,ChokeSize64th,WellheadPressurePsi,ArtificialLiftType,ArtificialLiftStatus\n" +
            $"{wellId},2026-01-01,100.5,400.2,20.1,24,250,SomeUnknownLift,SomeUnknownStatus\n";

        var result = await service.ImportProductionRecordsAsync(ToStream(csv));

        Assert.Equal(1, result.RowsImported);
        Assert.Equal(Domain.Enums.ArtificialLiftType.None, records.Records[0].ArtificialLiftType);
        Assert.Equal(Domain.Enums.ArtificialLiftStatus.Unknown, records.Records[0].ArtificialLiftStatus);
    }
}
