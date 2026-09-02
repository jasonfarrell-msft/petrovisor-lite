using PetroVisorLite.Application.Assistant;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;
using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Application.Tests.Assistant;

public class QueryOrchestratorTests
{
    private sealed class FakeKpiService : IKpiService
    {
        public Task<ProductionKpiDto> GetProductionKpiAsync(Guid wellId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductionKpiDto(wellId, periodStart, periodEnd, 100, 50, 10));

        public Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
        {
            var summary = new DashboardSummaryDto(
                WellCount: 3,
                FacilityCount: 1,
                TotalOilBbl30d: 1200,
                TotalGasMcf30d: 350,
                FieldDailyProduction:
                [
                    new FieldDailyProductionDto(periodStart, 100, 20, 10),
                    new FieldDailyProductionDto(periodStart.AddDays(1), 200, 30, 15),
                ],
                ArtificialLiftBreakdown:
                [
                    new ArtificialLiftBreakdownDto("Esp", 2, 900),
                    new ArtificialLiftBreakdownDto("RodPump", 1, 300),
                ],
                TopWellsByDecline:
                [
                    new WellDeclineRankingDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Well Alpha", "API-ALPHA", 0.12, 4.3),
                    new WellDeclineRankingDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Well Beta", "API-BETA", 0.08, 3.0),
                ]);

            return Task.FromResult(summary);
        }
    }

    private sealed class FakeWellRepository : IWellRepository
    {
        private readonly IReadOnlyList<Well> _wells;

        public FakeWellRepository(IReadOnlyList<Well> wells) => _wells = wells;

        public Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_wells.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Well>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_wells);

        public Task AddAsync(Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_TopWellsByDeclineRate_UsesBackendSummaryData()
    {
        var orchestrator = new QueryOrchestrator(new FakeKpiService(), new FakeWellRepository([]));

        var response = await orchestrator.ExecuteAsync(
            new QueryIntentClassification(
                QueryIntent.TopWellsByDeclineRate,
                new QueryIntentParameters(TopN: 2),
                true,
                "Approved intent"));

        Assert.True(response.IsSupported);
        var result = Assert.IsType<TopWellsByDeclineResponseDto>(response.Data);
        Assert.Equal(2, result.RequestedLimit);
        Assert.Equal(2, result.Wells.Count);
        Assert.Equal(0.12, result.Wells[0].DailyDeclinePercent);
    }

    [Fact]
    public async Task ExecuteAsync_WellsByArtificialLiftStatus_GroupsRealWellData()
    {
        var wells = new[]
        {
            new Well { Id = Guid.NewGuid(), Name = "Well A", ArtificialLiftStatus = ArtificialLiftStatus.Running, ArtificialLiftType = ArtificialLiftType.Esp },
            new Well { Id = Guid.NewGuid(), Name = "Well B", ArtificialLiftStatus = ArtificialLiftStatus.Maintenance, ArtificialLiftType = ArtificialLiftType.RodPump },
            new Well { Id = Guid.NewGuid(), Name = "Well C", ArtificialLiftStatus = ArtificialLiftStatus.Running, ArtificialLiftType = ArtificialLiftType.Esp },
        };

        var orchestrator = new QueryOrchestrator(new FakeKpiService(), new FakeWellRepository(wells));

        var response = await orchestrator.ExecuteAsync(
            new QueryIntentClassification(
                QueryIntent.WellsByArtificialLiftStatus,
                new QueryIntentParameters(),
                true,
                "Approved intent"));

        Assert.True(response.IsSupported);
        var result = Assert.IsType<WellsByArtificialLiftStatusResponseDto>(response.Data);
        Assert.Contains(result.Breakdowns, x => x.ArtificialLiftStatus == ArtificialLiftStatus.Running.ToString() && x.WellCount == 2);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedQuestion_ReturnsCantAnswerMessage()
    {
        var orchestrator = new QueryOrchestrator(new FakeKpiService(), new FakeWellRepository([]));

        var response = await orchestrator.ExecuteAsync(
            new QueryIntentClassification(
                QueryIntent.Unsupported,
                new QueryIntentParameters(),
                false,
                "Question is out of scope."));

        Assert.False(response.IsSupported);
        Assert.Equal("I can't answer that yet.", response.Message);
    }
}
