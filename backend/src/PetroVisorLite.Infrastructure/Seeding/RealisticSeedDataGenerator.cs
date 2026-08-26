using PetroVisorLite.Domain;
using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Infrastructure.Seeding;

/// <summary>
/// Generates a realistic synthetic Permian Basin (Delaware sub-basin — Reeves/Midland
/// County, TX) demo dataset: facilities, wells, and daily production records exhibiting
/// Arps hyperbolic/exponential decline, correlated GOR/WOR trends, choke and
/// wellhead-pressure behavior, day-to-day noise, and injected production-loss
/// events for downtime detection. See Obi-Wan's design note in
/// .squad/agents/obi-wan/history.md for the full methodology.
///
/// This class is pure data generation — it has no EF Core/DbContext
/// dependency and does not decide *when*/*whether* to seed (that gate lives
/// in <see cref="SeedData.SeedAsync"/> / Han's Program.cs wiring).
/// </summary>
public static class RealisticSeedDataGenerator
{
    /// <summary>Fixed seed for fully reproducible demo data across runs/environments.</summary>
    public const int Seed = 20260825;

    public sealed record GeneratedData(List<Facility> Facilities, List<Well> Wells, List<ProductionRecord> Records);

    /// <summary>
    /// Generates the full dataset anchored so the most recent production record is "today" (UTC),
    /// with each well's history stretching back to its (randomized) completion date.
    /// </summary>
    public static GeneratedData Generate(DateOnly? asOf = null)
    {
        var random = new Random(Seed);
        var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var facilities = BuildFacilities();
        var wellSpecs = BuildWellSpecs(facilities);

        var wells = new List<Well>();
        var records = new List<ProductionRecord>();

        foreach (var spec in wellSpecs)
        {
            var well = new Well
            {
                Id = Guid.NewGuid(),
                Name = spec.Name,
                ApiNumber = spec.ApiNumber,
                Latitude = spec.Latitude,
                Longitude = spec.Longitude,
                FacilityId = spec.Facility.Id,
                ArtificialLiftType = spec.LiftType,
                ArtificialLiftStatus = spec.LiftType == ArtificialLiftType.None
                    ? ArtificialLiftStatus.Unknown
                    : ArtificialLiftStatus.Running,
            };
            wells.Add(well);

            var wellRecords = GenerateWellProduction(random, well, spec, today);
            records.AddRange(wellRecords);

            // Reflect the *last* simulated day's lift status/type as the well's "current" state.
            if (wellRecords.Count > 0)
            {
                var last = wellRecords[^1];
                well.ArtificialLiftType = last.ArtificialLiftType;
                well.ArtificialLiftStatus = last.ArtificialLiftStatus;
            }
        }

        return new GeneratedData(facilities, wells, records);
    }

    // ---- Facilities -------------------------------------------------------

    private static List<Facility> BuildFacilities() => new()
    {
        new Facility { Id = Guid.NewGuid(), Name = "Wolfcamp Pad 14 Battery", Type = "Battery" },
        new Facility { Id = Guid.NewGuid(), Name = "Reeves County Gathering Station 3", Type = "GatheringStation" },
        new Facility { Id = Guid.NewGuid(), Name = "Bone Spring Central Tank Battery", Type = "Battery" },
    };

    // ---- Well specs ---------------------------------------------------------

    private sealed record WellSpec(
        string Name,
        string ApiNumber,
        Facility Facility,
        double Latitude,
        double Longitude,
        DateOnly SpudDate,
        DateOnly CompletionDate,
        bool IsHorizontal,
        ArtificialLiftType LiftType,
        double QiOilBblPerDay,
        double DiAnnualNominal,
        double ArpsB,
        double InitialGorScfPerBbl,
        double GorGrowthPerYear,
        double InitialWaterCutFraction,
        double WaterCutGrowthPerYear,
        (int StartDayOffset, int DurationDays, double Severity, string Kind)[] LossEvents);

    /// <summary>
    /// 14 wells across 3 Permian Basin (Reeves/Midland County style) facilities.
    /// API numbers follow the 14-digit format: state(42=TX)-county(389=Reeves,317=Midland)-well seq(00001..)-directional(00).
    /// </summary>
    private static List<WellSpec> BuildWellSpecs(List<Facility> facilities)
    {
        var padA = facilities[0]; // Wolfcamp Pad 14 Battery
        var padB = facilities[1]; // Reeves County Gathering Station 3
        var padC = facilities[2]; // Bone Spring Central Tank Battery

        // Base coordinates roughly in the Delaware Basin / Reeves & Midland County, TX.
        return new List<WellSpec>
        {
            new("Rustler Federal 14-1H", "42-389-38101-00", padA, 31.412, -103.512, new(2023, 2, 1), new(2023, 4, 10), true,
                ArtificialLiftType.Esp, 950, 0.72, 0.55, 850, 220, 0.12, 0.35,
                new[] { (210, 6, 0.85, "ESP failure") }),
            new("Rustler Federal 14-2H", "42-389-38102-00", padA, 31.413, -103.510, new(2023, 2, 1), new(2023, 4, 14), true,
                ArtificialLiftType.Esp, 880, 0.70, 0.52, 900, 240, 0.14, 0.38,
                Array.Empty<(int, int, double, string)>()),
            new("Rustler Federal 14-3H", "42-389-38103-00", padA, 31.414, -103.509, new(2023, 2, 3), new(2023, 4, 18), true,
                ArtificialLiftType.GasLift, 1020, 0.75, 0.58, 780, 200, 0.10, 0.32,
                new[] { (365, 4, 0.9, "Line freeze / gas lift injection outage") }),
            new("Rustler Federal 14-4H", "42-389-38104-00", padA, 31.410, -103.513, new(2023, 3, 1), new(2023, 5, 20), true,
                ArtificialLiftType.RodPump, 610, 0.60, 0.42, 1100, 260, 0.18, 0.42,
                Array.Empty<(int, int, double, string)>()),

            new("Salt Draw Unit 7-1H", "42-389-38201-00", padB, 31.388, -103.487, new(2022, 10, 5), new(2022, 12, 12), true,
                ArtificialLiftType.Esp, 1150, 0.78, 0.60, 700, 190, 0.11, 0.30,
                new[] { (95, 5, 0.8, "ESP trip / workover"), (480, 3, 0.6, "Facility downtime - offloading backlog") }),
            new("Salt Draw Unit 7-2H", "42-389-38202-00", padB, 31.389, -103.485, new(2022, 10, 5), new(2022, 12, 15), true,
                ArtificialLiftType.GasLift, 990, 0.73, 0.56, 820, 210, 0.13, 0.34,
                Array.Empty<(int, int, double, string)>()),
            new("Salt Draw Unit 7-3H", "42-389-38203-00", padB, 31.390, -103.484, new(2022, 10, 8), new(2022, 12, 19), true,
                ArtificialLiftType.RodPump, 540, 0.58, 0.40, 1200, 280, 0.20, 0.45,
                new[] { (300, 7, 0.7, "Rod parted") }),
            new("Salt Draw State 7-4", "42-389-38204-00", padB, 31.387, -103.489, new(2022, 11, 1), new(2023, 1, 8), false,
                ArtificialLiftType.RodPump, 210, 0.40, 0.30, 1400, 300, 0.30, 0.55,
                Array.Empty<(int, int, double, string)>()),
            new("Salt Draw State 7-5", "42-389-38205-00", padB, 31.386, -103.490, new(2022, 11, 3), new(2023, 1, 12), false,
                ArtificialLiftType.None, 140, 0.35, 0.25, 1500, 320, 0.25, 0.50,
                Array.Empty<(int, int, double, string)>()),

            new("Bone Spring Federal 22-1H", "42-317-40501-00", padC, 31.965, -102.078, new(2023, 5, 12), new(2023, 7, 22), true,
                ArtificialLiftType.Esp, 1080, 0.76, 0.57, 760, 200, 0.10, 0.28,
                new[] { (150, 5, 0.88, "ESP failure - VFD trip") }),
            new("Bone Spring Federal 22-2H", "42-317-40502-00", padC, 31.966, -102.076, new(2023, 5, 12), new(2023, 7, 26), true,
                ArtificialLiftType.Esp, 1010, 0.74, 0.54, 800, 215, 0.12, 0.31,
                Array.Empty<(int, int, double, string)>()),
            new("Bone Spring Federal 22-3H", "42-317-40503-00", padC, 31.967, -102.075, new(2023, 5, 15), new(2023, 7, 30), true,
                ArtificialLiftType.GasLift, 940, 0.71, 0.53, 830, 225, 0.13, 0.33,
                new[] { (60, 3, 0.75, "Compressor station outage") }),
            new("Bone Spring Federal 22-4H", "42-317-40504-00", padC, 31.963, -102.080, new(2023, 6, 1), new(2023, 8, 15), true,
                ArtificialLiftType.RodPump, 560, 0.57, 0.38, 1150, 270, 0.19, 0.44,
                Array.Empty<(int, int, double, string)>()),
            new("Bone Spring Federal 22-5", "42-317-40505-00", padC, 31.962, -102.082, new(2023, 6, 4), new(2023, 8, 19), false,
                ArtificialLiftType.Pcp, 260, 0.42, 0.30, 1350, 290, 0.22, 0.47,
                new[] { (200, 4, 0.65, "PCP rod failure") }),
            new("Bone Spring State 22-6", "42-317-40506-00", padC, 31.961, -102.083, new(2023, 6, 6), new(2023, 8, 23), false,
                ArtificialLiftType.RodPump, 190, 0.38, 0.28, 1450, 310, 0.28, 0.52,
                Array.Empty<(int, int, double, string)>()),
        };
    }

    // ---- Production generation ---------------------------------------------

    /// <summary>
    /// Generates one well's full daily production history from its completion date through
    /// <paramref name="today"/> using Arps hyperbolic decline (or exponential when b≈0),
    /// correlated GOR/WOR growth, choke/pressure behavior, day-to-day noise, and any
    /// scripted production-loss events.
    /// </summary>
    private static List<ProductionRecord> GenerateWellProduction(Random random, Well well, WellSpec spec, DateOnly today)
    {
        var records = new List<ProductionRecord>();
        var dayCount = today.DayNumber - spec.CompletionDate.DayNumber;
        if (dayCount < 30)
        {
            dayCount = 30; // guarantee at least ~30 days of history even for "recently completed" wells
        }

        // Convert nominal annual decline (Di) to a daily effective rate for the Arps formula.
        var diDaily = spec.DiAnnualNominal / 365.0;
        var b = spec.ArpsB;

        var liftStatus = ArtificialLiftStatus.Running;
        var liftType = spec.LiftType;

        for (var d = 0; d <= dayCount; d++)
        {
            var date = spec.CompletionDate.AddDays(d);

            // --- Arps hyperbolic decline: q(t) = qi / (1 + b*Di*t)^(1/b); falls back to
            // exponential q(t) = qi * exp(-Di*t) when b is ~0 (not used here but kept for completeness).
            double oilRate;
            if (b > 0.001)
            {
                oilRate = spec.QiOilBblPerDay / Math.Pow(1.0 + b * diDaily * d, 1.0 / b);
            }
            else
            {
                oilRate = spec.QiOilBblPerDay * Math.Exp(-diDaily * d);
            }

            // Early-life ramp-up (first ~10 days) as the well cleans up post-frac.
            if (d < 10)
            {
                oilRate *= 0.4 + 0.06 * d;
            }

            var yearsOnline = d / 365.0;

            // GOR rises over time (typical of Permian unconventional decline as reservoir pressure drops).
            var gorScfPerBbl = spec.InitialGorScfPerBbl + spec.GorGrowthPerYear * yearsOnline;

            // Water cut rises over time (increasing WOR as the well matures).
            var waterCut = Math.Min(0.92, spec.InitialWaterCutFraction + spec.WaterCutGrowthPerYear * yearsOnline);

            // Day-to-day operational noise (+/-8%).
            var noise = 0.92 + random.NextDouble() * 0.16;

            var productionMultiplier = 1.0;

            // --- Inject scripted production-loss events (downtime, equipment failure, etc.) ---
            foreach (var (startDayOffset, durationDays, severity, kind) in spec.LossEvents)
            {
                if (d >= startDayOffset && d < startDayOffset + durationDays)
                {
                    productionMultiplier = 1.0 - severity; // e.g. severity 0.85 => only 15% of expected rate
                    liftStatus = severity >= 0.8 ? ArtificialLiftStatus.Down : ArtificialLiftStatus.Maintenance;
                    _ = kind; // descriptive only; not persisted per-record (no free-text field on ProductionRecord)
                }
                else if (d == startDayOffset + durationDays && liftType != ArtificialLiftType.None)
                {
                    // Recovery day: back to Running once the event window ends.
                    liftStatus = ArtificialLiftStatus.Running;
                }
            }

            if (liftType == ArtificialLiftType.None)
            {
                liftStatus = ArtificialLiftStatus.Unknown;
            }

            var oil = Math.Max(0.0, oilRate * noise * productionMultiplier);
            var gas = Math.Max(0.0, oil * gorScfPerBbl / 1000.0); // Mcf, since GOR is scf/bbl
            var water = Math.Max(0.0, oil * (waterCut / Math.Max(0.01, 1 - waterCut)) * (0.95 + random.NextDouble() * 0.1)); // bbl, from WOR = waterCut/(1-waterCut)

            // Choke size (64ths) starts wide open and gets choked back as the well matures and pressure declines.
            var declineFraction = Math.Clamp(1.0 - oilRate / spec.QiOilBblPerDay, 0.0, 1.0);
            var chokeSize = spec.IsHorizontal
                ? Math.Round(Math.Clamp(48 - declineFraction * 24 + random.Next(-2, 3), 12, 48))
                : Math.Round(Math.Clamp(28 - declineFraction * 12 + random.Next(-2, 3), 8, 28));

            // Wellhead pressure correlates with decline stage and choke size — high early, falling over time.
            var basePressure = spec.IsHorizontal ? 1400 : 550;
            var pressure = Math.Max(30, basePressure * (1.0 - 0.75 * declineFraction) * (chokeSize / (spec.IsHorizontal ? 48.0 : 28.0))
                                     + random.Next(-25, 25));
            if (productionMultiplier < 0.5)
            {
                pressure *= 0.5; // pressure sags sharply during a loss event
            }

            records.Add(new ProductionRecord
            {
                Id = Guid.NewGuid(),
                WellId = well.Id,
                Date = date,
                OilVolumeBbl = Math.Round(oil, 2),
                GasVolumeMcf = Math.Round(gas, 2),
                WaterVolumeBbl = Math.Round(water, 2),
                ChokeSize64th = chokeSize,
                WellheadPressurePsi = Math.Round(pressure, 1),
                ArtificialLiftType = liftType,
                ArtificialLiftStatus = liftStatus,
            });
        }

        return records;
    }
}
