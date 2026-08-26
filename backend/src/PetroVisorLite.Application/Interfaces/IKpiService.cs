using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>
/// KPI computation contract. Obi-Wan owns the concrete implementation
/// (decline curves, water cut, uptime, etc.) built on top of the repositories.
/// </summary>
public interface IKpiService
{
    Task<ProductionKpiDto> GetProductionKpiAsync(Guid wellId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the single aggregate payload backing the Dashboard page (summary cards + 3 charts)
    /// in one call, instead of requiring a per-well KPI-fetch loop from the frontend.
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);
}
