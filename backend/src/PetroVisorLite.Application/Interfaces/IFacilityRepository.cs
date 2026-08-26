using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>Repository contract for Facility entities. Implemented in Infrastructure.</summary>
public interface IFacilityRepository
{
    Task<Facility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Facility>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Facility facility, CancellationToken cancellationToken = default);
    Task UpdateAsync(Facility facility, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
