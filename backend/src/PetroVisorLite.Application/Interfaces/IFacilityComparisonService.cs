using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>
/// Read-only contract for the facility comparison view (Story #11). Implementations must
/// return <see cref="FacilityComparisonDto.Facilities"/> ordered by the default comparison
/// order: total oil descending, with facility name ascending as the deterministic tie-breaker.
/// No BOE conversion is assumed here — that decision is explicitly deferred.
/// </summary>
public interface IFacilityComparisonService
{
    /// <summary>
    /// Builds the facility comparison payload for the given date range. Sprint 1's Blazor UI
    /// will always pass a fixed recent window, but the contract accepts an arbitrary range so
    /// a future configurable reporting window (Story #12) can reuse this service unchanged.
    /// </summary>
    Task<FacilityComparisonDto> GetFacilityComparisonAsync(DateOnly rangeStart, DateOnly rangeEnd, CancellationToken cancellationToken = default);
}
