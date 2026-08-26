using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Application.Analytics;

/// <summary>Sum of production volumes for a well over a single calendar day.</summary>
public record DailyProductionTotal(Guid WellId, DateOnly Date, double OilVolumeBbl, double GasVolumeMcf, double WaterVolumeBbl);

/// <summary>Sum of production volumes for a well over a single calendar month.</summary>
public record MonthlyProductionTotal(Guid WellId, int Year, int Month, double OilVolumeBbl, double GasVolumeMcf, double WaterVolumeBbl);

/// <summary>
/// Estimated decline rate for a well's oil stream over a given window.
/// <see cref="DailyDeclinePercent"/> is the fitted exponential decline rate expressed as a
/// fraction lost per day (e.g. 0.01 = 1%/day decline). Positive values mean production is
/// declining; negative values mean it is increasing (rate reversal e.g. workover, new perfs).
/// </summary>
public record DeclineRateResult(Guid WellId, DateOnly PeriodStart, DateOnly PeriodEnd, double DailyDeclinePercent, double AnnualDeclinePercent, int PointsUsed);

/// <summary>A single day flagged as a production loss (actual materially below rolling-average baseline).</summary>
public record ProductionLossFlag(Guid WellId, DateOnly Date, double ActualOilVolumeBbl, double ExpectedOilVolumeBbl, double PercentBelowExpected);

/// <summary>An artificial lift event of interest: a status/type change and/or a correlated output drop.</summary>
public record ArtificialLiftFlag(
    Guid WellId,
    DateOnly Date,
    ArtificialLiftType PreviousLiftType,
    ArtificialLiftType CurrentLiftType,
    ArtificialLiftStatus PreviousLiftStatus,
    ArtificialLiftStatus CurrentLiftStatus,
    bool IsStatusOrTypeChange,
    bool IsCorrelatedWithProductionDrop,
    string Reason);
