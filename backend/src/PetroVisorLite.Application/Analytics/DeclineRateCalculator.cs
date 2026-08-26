using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <inheritdoc cref="IDeclineRateCalculator"/>
public class DeclineRateCalculator : IDeclineRateCalculator
{
    public DeclineRateResult? CalculateDeclineRate(IEnumerable<ProductionRecord> wellHistory, int minimumPoints = 5)
    {
        ArgumentNullException.ThrowIfNull(wellHistory);

        var ordered = wellHistory
            .Where(r => r.OilVolumeBbl > 0) // exclude zero/negative days: undefined in log-linear fit, usually shut-ins
            .OrderBy(r => r.Date)
            .ToList();

        if (ordered.Count < minimumPoints)
        {
            return null;
        }

        var wellId = ordered[0].WellId;
        var t0 = ordered[0].Date;

        // Linear regression of y = ln(oil volume) against x = days since first point.
        // Slope of that regression is -D (the exponential decline constant per day).
        var n = ordered.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;

        foreach (var record in ordered)
        {
            double x = record.Date.DayNumber - t0.DayNumber;
            double y = Math.Log(record.OilVolumeBbl);
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumXX += x * x;
        }

        double denominator = (n * sumXX) - (sumX * sumX);
        double slope = denominator == 0 ? 0 : ((n * sumXY) - (sumX * sumY)) / denominator;

        // Decline rate is the negative of the fitted slope, expressed as a fraction/day.
        double dailyDecline = -slope;
        // Compounded annualized figure (not simple multiplication) to stay consistent with the
        // exponential model: (1 - annualDecline) = (1 - dailyDecline)^365.
        double annualDecline = 1 - Math.Pow(1 - dailyDecline, 365);

        var periodStart = ordered[0].Date;
        var periodEnd = ordered[^1].Date;

        return new DeclineRateResult(wellId, periodStart, periodEnd, dailyDecline, annualDecline, n);
    }
}
