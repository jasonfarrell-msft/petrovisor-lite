namespace PetroVisorLite.Application.Dtos;

/// <summary>
/// Example KPI result DTO — Obi-Wan's KPI service will return shapes like this.
/// Extend with additional KPIs (decline rate, water cut, uptime, etc.).
/// </summary>
public record ProductionKpiDto(Guid WellId, DateOnly PeriodStart, DateOnly PeriodEnd, double TotalOilBbl, double TotalGasMcf, double AverageDailyOilBbl);
