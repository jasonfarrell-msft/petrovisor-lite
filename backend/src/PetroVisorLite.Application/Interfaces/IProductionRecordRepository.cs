using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>Repository contract for ProductionRecord entities. Implemented in Infrastructure by Han.</summary>
public interface IProductionRecordRepository
{
    Task<IReadOnlyList<ProductionRecord>> GetByWellIdAsync(Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ProductionRecord> records, CancellationToken cancellationToken = default);
}
