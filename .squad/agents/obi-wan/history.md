# Obi-Wan — History

## Project Context
PetroVisor Lite analytics: KPI service in the Application layer — daily/monthly production totals, decline rate, production loss detection (rolling average comparison), artificial lift status flags. Must be swappable/extensible for future ML.NET models. xUnit tests required.

Requested by: jasonfarrell. Session start: 2026-08-25.

## 2026-08-25 — KPI/Analytics service implemented

Built `PetroVisorLite.Application/Analytics/` with four focused,
framework-agnostic interfaces (all operate on plain `IEnumerable<ProductionRecord>`,
no EF Core/persistence deps):

- `IProductionKpiCalculator` (`ProductionKpiCalculator`) — daily/monthly
  production totals per well.
- `IDeclineRateCalculator` (`DeclineRateCalculator`) — exponential decline
  rate via log-linear regression (Arps b=0 special case); documented
  limitations vs. full hyperbolic/harmonic Arps analysis.
- `IProductionLossDetector` (`ProductionLossDetector`) — flags days >X%
  below a trailing rolling-average baseline (configurable window/threshold).
- `IArtificialLiftMonitor` (`ArtificialLiftMonitor`) — flags lift
  type/status changes and correlates with coincident production drops.

Kept Leia's `IKpiService` interface as-is; implemented it as `KpiService`, a
thin facade over `IProductionRecordRepository` + `IProductionKpiCalculator`.

Added `ServiceCollectionExtensions.AddApplicationServices()` in a new
`DependencyInjection` folder in the Application project (no existing DI
pattern was set up yet in Program.cs) — Han should call it from Program.cs
alongside Infrastructure/repository registrations.

Added 15 xUnit tests total (14 new + 1 existing smoke test) in
`tests/PetroVisorLite.Application.Tests/Analytics/`, covering: daily/monthly
totals, decline rate on synthetic declining/increasing/shut-in series, loss
detection (injected 50% drop flagged, normal noise not flagged, warm-up
window respected), lift monitor (status change + correlated drop, type
change without drop, no-change baseline), and DI registration resolves
`IKpiService`. `dotnet test` — 15/15 passing.

Full solution build currently fails due to an unrelated in-progress error in
Han's `PetroVisorLite.Infrastructure` DI extension (missing
`Microsoft.AspNetCore.Authentication` reference) — not in my scope; flagged
in my decision file. Application + Application.Tests build clean in
isolation.

Decision recorded at `.squad/decisions/inbox/obi-wan-analytics.md`.

## 2026-08-25: Realistic Synthetic Seed Data (Permian Basin, Delaware sub-basin)

**What:** Designed and implemented `RealisticSeedDataGenerator` (`backend/src/PetroVisorLite.Infrastructure/Seeding/RealisticSeedDataGenerator.cs`) — a pure, DbContext-free data generator producing 3 facilities and 14 wells across the Permian Basin's Delaware sub-basin (Reeves County and Midland County, TX), each with 30 days to ~22 months of daily production history (each well runs from its own completion date through "today", so history length varies realistically by spud vintage — the oldest wells, spudded Oct 2022, have ~22 months; the youngest, spudded June 2023, have ~14 months; all exceed the 30-day KPI minimum).

**Basin choice:** Delaware Basin (Permian), Reeves/Midland Counties, TX — consistent lat/long (~31.4°N/-103.5°W for Reeves, ~31.96°N/-102.08°W for Midland), 14-digit TX Railroad Commission-style API numbers (`42-<county>-<sequence>-<directional>`, 42 = Texas, 389 = Reeves, 317 = Midland). Well names follow standard federal/unit lease naming (e.g. "Rustler Federal 14-1H", "Salt Draw Unit 7-3H", "Bone Spring Federal 22-1H"), mixing horizontal (10 wells, Wolfcamp/Bone Spring targets) and vertical/legacy wells (4 wells) with a realistic artificial lift mix (ESP, Gas Lift, Rod Pump, PCP, and one Natural Flow/None).

**Decline curve model:** Arps hyperbolic decline, `q(t) = qi / (1 + b·Di·t)^(1/b)`, with per-well `qi` (initial oil rate, 140–1150 bbl/d), nominal annual `Di` (0.35–0.78, typical for Permian unconventional horizontals), and `b` exponents (0.25–0.60) — vertical/legacy wells get lower b (more exponential-like, faster early decline) and horizontals get higher b (flatter long-tail decline), matching real-world Wolfcamp/Bone Spring type curves. A 10-day post-frac cleanup ramp-up precedes the decline. GOR rises linearly with time online (700–1500 scf/bbl trending up 190–320 scf/bbl/yr) and water cut rises similarly (10–30% initial, growing toward a 92% cap) — both standard signs of reservoir pressure depletion and water breakthrough in Permian unconventional wells. Choke size (64ths) and wellhead pressure are derived from the well's current decline fraction (wide open/high pressure early, choked back/low pressure late) rather than sampled independently, so they stay physically correlated with the decline stage. ±8–16% daily noise is layered on top of every volume/pressure figure so curves aren't perfectly smooth.

**Production-loss injection:** 8 of the 14 wells carry one or two scripted loss events (`LossEvents` tuples: day offset, duration, severity, descriptive kind) — ESP/VFD trips, gas-lift/compressor outages, rod parts, line freezes, and one shared facility-level "offloading backlog" event — each dropping production to 10–40% of expected rate for 3–7 days and correspondingly forcing `ArtificialLiftStatus` to `Down` (severe) or `Maintenance` (partial) with a sympathetic wellhead-pressure sag, then recovering to `Running`. This gives `IProductionLossDetector`'s rolling-average/threshold logic and `IArtificialLiftMonitor`'s status-change/correlated-drop logic real, varied signal across multiple wells and lift types rather than the single scripted event in the original `SeedData.cs`. `Seed = 20260825` (fixed `Random` seed) makes the whole dataset fully reproducible.

**Handoff:** This file only adds `RealisticSeedDataGenerator`; it does not touch `SeedData.cs`, `Program.cs`, DI, or the Development-only gate — Han owns wiring it in (e.g. calling `RealisticSeedDataGenerator.Generate()` and persisting the result, replacing or supplementing the current hand-rolled 8-well seed). Verified `dotnet build` on `PetroVisorLite.Infrastructure` clean (0 errors/warnings).
