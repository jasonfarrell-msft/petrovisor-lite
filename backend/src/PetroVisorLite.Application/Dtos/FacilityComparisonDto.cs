namespace PetroVisorLite.Application.Dtos;

/// <summary>
/// Single facility's row in the facility comparison view (Story #11). Recent oil/gas/water
/// totals are computed over the requested date range; <c>HasNoReportedProduction</c> is true
/// when the facility's wells reported zero production records in that range, so the UI can
/// render an explicit "no data" indicator instead of a misleading zero.
/// </summary>
public record FacilityComparisonRowDto(
    Guid FacilityId,
    string FacilityName,
    string FacilityType,
    int TiedInWellCount,
    double TotalOilBbl,
    double TotalGasMcf,
    double TotalWaterBbl,
    bool HasNoReportedProduction);

/// <summary>
/// Read-only payload backing the facility comparison view. Sprint 1 UI uses a fixed recent
/// window, but the contract accepts an explicit date range so the service can be reused once
/// Story #12 introduces a configurable reporting window — that UI work stays out of scope here.
/// Rows are returned pre-sorted using the default comparison order: total oil descending, with
/// facility name ascending as the deterministic tie-breaker. No BOE (barrels of oil equivalent)
/// conversion is assumed or applied; oil, gas, and water totals remain separate.
/// </summary>
public record FacilityComparisonDto(
    DateOnly RangeStart,
    DateOnly RangeEnd,
    IReadOnlyList<FacilityComparisonRowDto> Facilities);
