using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <summary>
/// Aggregates raw <see cref="ProductionRecord"/> readings into daily/monthly totals per well.
/// Pure in-memory computation — no persistence/HTTP concerns, safe to unit test directly.
/// </summary>
public interface IProductionKpiCalculator
{
    IReadOnlyList<DailyProductionTotal> CalculateDailyTotals(IEnumerable<ProductionRecord> records);

    IReadOnlyList<MonthlyProductionTotal> CalculateMonthlyTotals(IEnumerable<ProductionRecord> records);
}

/// <summary>
/// Estimates a well's production decline rate from its historical oil volumes.
///
/// <para><b>Method:</b> fits a simple exponential decline model q(t) = q0 * e^(-D*t) via
/// linear regression on ln(oil volume) vs. day-offset. This is the classic "exponential decline"
/// special case of Arps' decline curve equations (b = 0) and is a reasonable first approximation
/// for a mature well with reasonably steady operating conditions.</para>
///
/// <para><b>Limitations:</b> a single global exponential fit does not capture hyperbolic/harmonic
/// declines (Arps b in (0,1]), does not account for shut-ins, workovers, choke changes, or
/// artificial-lift changes that can create step changes not explained by decline alone, and is
/// sensitive to noisy/zero-volume days (zero/negative volumes are excluded from the log-linear fit).
/// A "full PetroVisor" would fit hyperbolic/harmonic Arps curves (or use more advanced type-curve /
/// probabilistic decline analysis) and would segment history around operational events before fitting.</para>
/// </summary>
public interface IDeclineRateCalculator
{
    /// <summary>
    /// Returns null if there are fewer than <paramref name="minimumPoints"/> usable (positive-volume)
    /// data points in the supplied history — not enough data for a meaningful fit.
    /// </summary>
    DeclineRateResult? CalculateDeclineRate(IEnumerable<ProductionRecord> wellHistory, int minimumPoints = 5);
}

/// <summary>
/// Flags days where actual oil production is significantly below an "expected" baseline derived
/// from a trailing rolling average.
///
/// <para><b>Method:</b> for each day (after an initial warm-up window), computes the trailing
/// average of the previous <c>WindowSizeDays</c> days (excluding the day being evaluated) as the
/// expected baseline, then flags the day if actual volume is more than <c>ThresholdPercent</c>
/// below that baseline.</para>
///
/// <para><b>Limitations:</b> a simple trailing mean does not account for expected seasonal/decline
/// trends (a genuinely declining well will eventually false-flag against its own rolling average as
/// the decline steepens), does not distinguish planned downtime from unplanned loss, and is sensitive
/// to the window size chosen. A "full PetroVisor" would compare against a decline-curve-adjusted
/// expectation and/or a trained anomaly-detection model (see ML.NET integration note below).</para>
/// </summary>
public interface IProductionLossDetector
{
    IReadOnlyList<ProductionLossFlag> DetectLosses(
        IEnumerable<ProductionRecord> wellHistory,
        int windowSizeDays = 7,
        double thresholdPercent = 0.15);
}

/// <summary>
/// Flags artificial lift status/type changes and correlates them with production drops.
///
/// <para><b>Method:</b> walks a well's chronological history; whenever <c>ArtificialLiftType</c>
/// or <c>ArtificialLiftStatus</c> changes from the prior reading, emits a flag. The flag is marked
/// as "correlated with production drop" if the oil volume on the change day (or the following day,
/// to allow for a one-day lag) is more than <c>ProductionDropThresholdPercent</c> below the trailing
/// average oil volume of the prior <c>BaselineWindowDays</c> readings under the *previous* lift
/// type/status.</para>
///
/// <para><b>Limitations:</b> this is a simple rule-based correlation (change + coincident drop), not
/// a causal analysis — it does not rule out coincidental declines, and does not compare lift-type
/// cohorts against each other. A "full PetroVisor" would maintain per-lift-type performance baselines
/// across the well population and use statistical/ML-based change-point detection.</para>
/// </summary>
public interface IArtificialLiftMonitor
{
    IReadOnlyList<ArtificialLiftFlag> DetectLiftEvents(
        IEnumerable<ProductionRecord> wellHistory,
        int baselineWindowDays = 7,
        double productionDropThresholdPercent = 0.15);
}
