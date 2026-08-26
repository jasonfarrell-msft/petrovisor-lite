using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Application.DependencyInjection;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Application.Tests.Analytics;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationServices_RegistersIKpiService_WhenRepositoryIsProvided()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddScoped<IProductionRecordRepository>(_ => new FakeProductionRecordRepository());
        services.AddScoped<IWellRepository>(_ => new FakeWellRepository());

        var provider = services.BuildServiceProvider();
        var kpiService = provider.GetService<IKpiService>();

        Assert.NotNull(kpiService);
    }

    private class FakeProductionRecordRepository : IProductionRecordRepository
    {
        public Task<IReadOnlyList<PetroVisorLite.Domain.ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PetroVisorLite.Domain.ProductionRecord>>(Array.Empty<PetroVisorLite.Domain.ProductionRecord>());

        public Task AddRangeAsync(IEnumerable<PetroVisorLite.Domain.ProductionRecord> records, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class FakeWellRepository : IWellRepository
    {
        public Task<PetroVisorLite.Domain.Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<PetroVisorLite.Domain.Well?>(null);

        public Task<IReadOnlyList<PetroVisorLite.Domain.Well>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PetroVisorLite.Domain.Well>>(Array.Empty<PetroVisorLite.Domain.Well>());

        public Task AddAsync(PetroVisorLite.Domain.Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(PetroVisorLite.Domain.Well well, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
