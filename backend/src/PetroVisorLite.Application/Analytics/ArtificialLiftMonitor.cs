using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <inheritdoc cref="IArtificialLiftMonitor"/>
public class ArtificialLiftMonitor : IArtificialLiftMonitor
{
    public IReadOnlyList<ArtificialLiftFlag> DetectLiftEvents(
        IEnumerable<ProductionRecord> wellHistory,
        int baselineWindowDays = 7,
        double productionDropThresholdPercent = 0.15)
    {
        ArgumentNullException.ThrowIfNull(wellHistory);
        if (baselineWindowDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(baselineWindowDays), "Baseline window must be at least 1 day.");
        }

        var ordered = wellHistory.OrderBy(r => r.Date).ToList();
        var flags = new List<ArtificialLiftFlag>();

        for (int i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];

            bool typeChanged = previous.ArtificialLiftType != current.ArtificialLiftType;
            bool statusChanged = previous.ArtificialLiftStatus != current.ArtificialLiftStatus;

            if (!typeChanged && !statusChanged)
            {
                continue;
            }

            // Baseline = trailing average oil volume under the *previous* lift configuration,
            // looking back up to baselineWindowDays (or fewer if less history is available).
            int windowStart = Math.Max(0, i - baselineWindowDays);
            var baselineWindow = ordered.Skip(windowStart).Take(i - windowStart).ToList();
            double baseline = baselineWindow.Count > 0 ? baselineWindow.Average(r => r.OilVolumeBbl) : 0;

            // Allow a one-day lag: check the change day itself, and the following day if present,
            // for a coincident production drop (equipment changes can take a day to affect output).
            double changeDayVolume = current.OilVolumeBbl;
            double? nextDayVolume = i + 1 < ordered.Count ? ordered[i + 1].OilVolumeBbl : null;

            bool changeDayDropped = baseline > 0 && (baseline - changeDayVolume) / baseline > productionDropThresholdPercent;
            bool nextDayDropped = baseline > 0 && nextDayVolume is double nd && (baseline - nd) / baseline > productionDropThresholdPercent;
            bool correlated = changeDayDropped || nextDayDropped;

            string reason = BuildReason(typeChanged, statusChanged, correlated);

            flags.Add(new ArtificialLiftFlag(
                current.WellId,
                current.Date,
                previous.ArtificialLiftType,
                current.ArtificialLiftType,
                previous.ArtificialLiftStatus,
                current.ArtificialLiftStatus,
                IsStatusOrTypeChange: true,
                IsCorrelatedWithProductionDrop: correlated,
                Reason: reason));
        }

        return flags;
    }

    private static string BuildReason(bool typeChanged, bool statusChanged, bool correlated)
    {
        var parts = new List<string>();
        if (typeChanged) parts.Add("lift type changed");
        if (statusChanged) parts.Add("lift status changed");
        if (correlated) parts.Add("coincident production drop vs. prior baseline");
        return string.Join("; ", parts);
    }
}
