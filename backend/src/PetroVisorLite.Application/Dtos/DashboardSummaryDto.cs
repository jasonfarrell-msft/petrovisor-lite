namespace PetroVisorLite.Application.Dtos;

/// <summary>
/// Field-wide daily production totals for a single calendar day, aggregated across all wells.
/// Backs the Dashboard's "field-wide daily production trend" chart.
/// </summary>
public record FieldDailyProductionDto(DateOnly Date, double OilVolumeBbl, double GasVolumeMcf, double WaterVolumeBbl);

/// <summary>
/// Aggregated production/well-count totals for a single artificial lift type.
/// Backs the Dashboard's "production by artificial lift type" chart.
/// </summary>
public record ArtificialLiftBreakdownDto(string ArtificialLiftType, int WellCount, double TotalOilBbl30d);

/// <summary>
/// A well's estimated decline rate over the trailing period, for ranking wells by decline severity.
/// Backs the Dashboard's "top wells by decline rate" chart. <c>DailyDeclinePercent</c> is null when
/// there isn't enough usable production history to fit a decline curve for the well.
/// </summary>
public record WellDeclineRankingDto(Guid WellId, string WellName, string ApiNumber, double? DailyDeclinePercent, double? AnnualDeclinePercent);

/// <summary>
/// Single aggregate payload for the Dashboard page: everything needed to render the 4 summary
/// cards and 3 charts in one round trip, instead of the previous per-well KPI-fetch loop.
/// </summary>
public record DashboardSummaryDto(
    int WellCount,
    int FacilityCount,
    double TotalOilBbl30d,
    double TotalGasMcf30d,
    IReadOnlyList<FieldDailyProductionDto> FieldDailyProduction,
    IReadOnlyList<ArtificialLiftBreakdownDto> ArtificialLiftBreakdown,
    IReadOnlyList<WellDeclineRankingDto> TopWellsByDecline);
