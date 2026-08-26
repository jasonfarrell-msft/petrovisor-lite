using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>Repository contract for Well aggregate. Implemented in Infrastructure by Han.</summary>
public interface IWellRepository
{
    Task<Well?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Well>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Well well, CancellationToken cancellationToken = default);
    Task UpdateAsync(Well well, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
