using PetroVisorLite.Web.Models;

namespace PetroVisorLite.Web.Analytics;

/// <summary>
/// Lightweight, client-side approximations of the backend's decline-rate / production-loss
/// analytics (see backend PetroVisorLite.Application.Analytics), used on the Well Detail page
/// since those calculators are not yet exposed as their own API endpoints — only folded into
/// KpiService's totals. Methodology intentionally mirrors the backend's documented approach
/// (log-linear exponential decline fit; trailing-average rolling-window loss threshold) so the
/// two stay conceptually consistent even though this is a simplified frontend-side estimate.
/// </summary>
public static class ProductionTrendAnalyzer
{
    /// <summary>
    /// Fits q(t) = q0 * e^(-D*t) via ordinary least-squares linear regression of ln(oil volume)
    /// against day-offset. Returns the daily decline fraction D (positive = declining), or null
    /// if there are fewer than 5 usable (positive-volume) points.
    /// </summary>
    public static double? EstimateDailyDeclinePercent(IReadOnlyList<ProductionRecordDto> records)
    {
        var points = records
            .Select((r, i) => (X: (double)i, Y: r.OilVolumeBbl))
            .Where(p => p.Y > 0)
            .ToList();

        if (points.Count < 5)
        {
            return null;
        }

        var n = points.Count;
        var sumX = points.Sum(p => p.X);
        var sumY = points.Sum(p => Math.Log(p.Y));
        var sumXY = points.Sum(p => p.X * Math.Log(p.Y));
        var sumXX = points.Sum(p => p.X * p.X);

        var denominator = n * sumXX - sumX * sumX;
        if (denominator == 0)
        {
            return null;
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        return -slope;
    }

    /// <summary>
    /// Counts days whose oil volume falls more than <paramref name="thresholdPercent"/> below the
    /// trailing <paramref name="windowSizeDays"/>-day average (excluding the evaluated day itself).
    /// </summary>
    public static int CountLossFlags(IReadOnlyList<ProductionRecordDto> records, int windowSizeDays = 7, double thresholdPercent = 0.15)
    {
        var flagCount = 0;
        for (var i = windowSizeDays; i < records.Count; i++)
        {
            var window = records.Skip(i - windowSizeDays).Take(windowSizeDays);
            var expected = window.Average(r => r.OilVolumeBbl);
            if (expected <= 0)
            {
                continue;
            }

            var actual = records[i].OilVolumeBbl;
            if (actual < expected * (1 - thresholdPercent))
            {
                flagCount++;
            }
        }

        return flagCount;
    }
}
