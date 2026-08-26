using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <inheritdoc cref="IProductionLossDetector"/>
public class ProductionLossDetector : IProductionLossDetector
{
    public IReadOnlyList<ProductionLossFlag> DetectLosses(
        IEnumerable<ProductionRecord> wellHistory,
        int windowSizeDays = 7,
        double thresholdPercent = 0.15)
    {
        ArgumentNullException.ThrowIfNull(wellHistory);
        if (windowSizeDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSizeDays), "Window size must be at least 1 day.");
        }
        if (thresholdPercent is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdPercent), "Threshold percent must be between 0 and 1 (exclusive).");
        }

        var ordered = wellHistory.OrderBy(r => r.Date).ToList();
        var flags = new List<ProductionLossFlag>();

        for (int i = 0; i < ordered.Count; i++)
        {
            // Trailing window strictly before the evaluated day — need a full window's worth of
            // prior history before we can compute a meaningful baseline.
            if (i < windowSizeDays)
            {
                continue;
            }

            var window = ordered.Skip(i - windowSizeDays).Take(windowSizeDays);
            double expected = window.Average(r => r.OilVolumeBbl);

            if (expected <= 0)
            {
                continue; // no meaningful baseline (e.g. well was shut in for the whole window)
            }

            var current = ordered[i];
            double percentBelow = (expected - current.OilVolumeBbl) / expected;

            if (percentBelow > thresholdPercent)
            {
                flags.Add(new ProductionLossFlag(current.WellId, current.Date, current.OilVolumeBbl, expected, percentBelow));
            }
        }

        return flags;
    }
}
