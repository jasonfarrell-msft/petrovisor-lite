# Luke — History

## Project Context
PetroVisor Lite frontend: React + TypeScript, calls REST API, Recharts/Chart.js for production trend visualization, well list + dashboard, JWT-based auth context/hook, role-based UI (Engineer/Viewer), Jest/RTL tests, `.env` config excluded from source control.

Requested by: jasonfarrell. Session start: 2026-08-25.

## 2026-08-25 — Pivot to ASP.NET Core Razor Pages (npm registry blocked)

The React/npm approach got stuck for over an hour on package resolution —
this corp machine can't reach the public npm registry (only a private MS
feed proxy, which doesn't mirror everything the Vite/React toolchain needed).
The user decided to abandon React/npm entirely; that work was moved to
`/old-frontend` (untouched, kept for reference).

Built the new frontend as `frontend/PetroVisorLite.Web`, an ASP.NET Core
Razor Pages app (net10.0, matching the backend), plus
`frontend/tests/PetroVisorLite.Web.Tests` (xUnit), tied together by
`frontend/PetroVisorLite.Frontend.sln`. Verified NuGet works fine in this
environment before committing to the approach.

- Pages: Login, Logout, Wells/Index (list), Wells/Detail (Chart.js
  production trend via CDN + KPI summary incl. decline rate/loss
  flags/artificial lift status), Dashboard (key metric cards + wells table),
  Import (Engineer-only CSV upload).
- Auth bridging: Login posts to backend `/api/auth/login`; the returned JWT
  + roles are mapped (`Auth/JwtClaimsMapper`) into ASP.NET Core cookie-auth
  claims, and a `JwtForwardingHandler` `DelegatingHandler` re-attaches the
  stashed JWT as a Bearer token on every server-side call the Razor app
  makes to the backend API via a typed `PetroVisorApiClient`
  (`IHttpClientFactory`). `[Authorize(Roles = "Engineer")]` gates the Import
  page; Viewer gets read-only pages.
- Charting via Chart.js CDN `<script>` tag (not npm) — chart data serialized
  to JSON server-side, rendered client-side. Bootstrap via CDN `<link>` tags,
  replacing the scaffold's local `wwwroot/lib`.
- Backend only exposes production *totals* via KPI endpoint (no decline
  rate/loss/lift-event endpoints yet), so added
  `Web/Analytics/ProductionTrendAnalyzer` — a lightweight client-side
  approximation of Obi-Wan's documented decline-rate/loss-detection methods
  — as a stopgap for the Well Detail page; flagged as a backend follow-up.
- Tests: 10/10 passing (`JwtClaimsMapperTests` x5, `ProductionTrendAnalyzerTests`
  x5 — including recovering a known synthetic exponential decline rate).
- `dotnet build`: 0 warnings/errors. `dotnet run` (no launch profile, custom
  port) started cleanly; `/Login`, `/`, `/Wells/Index` all returned 200.
- Updated top-level `README.md` frontend section (Razor Pages / `dotnet run`
  instead of npm) and recorded the full rationale in
  `.squad/decisions/inbox/luke-frontend-pivot.md`.

## 2026-08-25 — Pivot to Blazor WebAssembly (Static Web Apps needs static output)

Second frontend pivot (React → Razor Pages → **Blazor WebAssembly**), driven
by the deployment target: the backend is going to Azure Container Apps, but
the frontend is going to **Azure Static Web Apps**, which only serves static
files (+ optional Functions API) — it cannot host the Razor Pages app's
required Kestrel/ASP.NET Core server process.

Rebuilt `frontend/PetroVisorLite.Web` from a `dotnet new blazorwasm`
standalone template (net10.0), replacing all Razor Pages files. Both
`Blazored.LocalStorage` and `bunit` resolved cleanly via NuGet — no fallback
needed.

- Auth: `Login.razor` posts to backend `/api/auth/login`, stores the JWT in
  `localStorage` via `Blazored.LocalStorage`. `Auth/JwtParser.cs` (pure,
  unit-testable) decodes claims/checks expiry. `Auth/
  JwtAuthenticationStateProvider.cs` builds the `ClaimsPrincipal` driving
  `<AuthorizeView>`/`[Authorize(Roles = "Engineer")]`. `Services/
  JwtAuthorizationHandler.cs` (a `DelegatingHandler`) attaches the stored JWT
  as a bearer token on every `PetroVisorApiClient` call.
- Chart.js via JS interop: `wwwroot/js/chartInterop.js` (CDN Chart.js, no
  npm) wrapped by `Services/ChartInteropService.cs`, called from
  `Pages/WellDetail.razor` via `IJSRuntime`.
- Pages ported 1:1: Home, Login, Wells, WellDetail (KPI + chart +
  `ProductionTrendAnalyzer` decline/loss stopgap, unchanged from pivot #1),
  Dashboard, Import (Engineer-only, uses Blazor's `InputFile`).
- Config: `wwwroot/appsettings.json`/`appsettings.Development.json` carry
  `BackendApi:BaseUrl` (no server-side env at WASM runtime).
- Tests: 17/17 passing — `JwtParserTests` (6), `JwtAuthenticationStateProviderTests`
  (4, against a hand-rolled `FakeLocalStorageService` since bUnit's loose
  JSInterop mode no-ops localStorage calls), `ProductionTrendAnalyzerTests`
  (5, ported unchanged), and one real `bunit` component-render test
  (`LoginComponentTests`) proving the bUnit path works.
- `dotnet build`: 0 errors/warnings. `dotnet publish -c Release` succeeds;
  static output confirmed at
  `frontend/PetroVisorLite.Web/bin/Release/net10.0/publish/wwwroot`
  (index.html, `_framework/`, css/js, appsettings.json — no server needed).
  `dotnet run` dev server sanity-checked: `/` and `/appsettings.json` both
  returned 200.
- Updated top-level `README.md` frontend section for Blazor WASM
  (setup/build/publish) and recorded full rationale + the exact publish path
  for Lando in `.squad/decisions/inbox/luke-blazor-wasm-pivot.md`.

## 2026-08-25 - Default landing page changed to Dashboard
- Removed Pages/Home.razor (trivial welcome page with link to /dashboard).
- Added `@page "/"` to Dashboard.razor alongside existing `@page "/dashboard"`, so root URL now renders Dashboard directly.
- MainLayout.razor nav already only linked to dashboard/wells/import (no separate Home link) — no changes needed there; navbar-brand href="" now lands on Dashboard too.
- Build succeeded (dotnet build on PetroVisorLite.Frontend.slnx), 0 errors.
- Tests: PetroVisorLite.Web.Tests — 17/17 passed.

## 2026-08-25 — Dashboard: replace well table with 3 charts

Replaced the "Wells by average daily oil (30d)" table (and its slow per-well
KPI-fetch loop in Dashboard.razor's OnInitializedAsync) with three Chart.js
charts, reusing the existing ChartInteropService/chartInterop.js JS-interop
pattern from WellDetail.razor (no new charting dependency).

Backend changes:
- Added `DashboardSummaryDto` (+ `FieldDailyProductionDto`, `ArtificialLiftBreakdownDto`,
  `WellDeclineRankingDto`) to Application/Dtos.
- Added `IKpiService.GetDashboardSummaryAsync` + implementation in `KpiService`
  (now also depends on `IWellRepository` and `IDeclineRateCalculator`). Computes
  well/facility counts, 30d oil/gas totals, field-wide daily totals, per-lift-type
  well count + oil totals, and top-10 wells by fitted decline rate — all from data
  already fetched once per well (no N+1 growth beyond the existing per-well history
  fetch, but now server-side and in one HTTP round trip for the frontend).
- Added `GET api/kpi/dashboard?periodStart=&periodEnd=` in `KpiController`.
- Added `KpiServiceDashboardSummaryTests` and updated `ServiceCollectionExtensionsTests`
  fake registrations for the new `IWellRepository` dependency.

Frontend changes:
- Added matching DTOs to `Models/ApiModels.cs` and `PetroVisorApiClient.GetDashboardSummaryAsync`.
- Added `renderFieldTrendChart`/`renderLiftBreakdownChart`/`renderDeclineRankingChart`
  (+ destroy counterparts) to `wwwroot/js/chartInterop.js`, and matching wrapper
  methods on `ChartInteropService`.
- Rewrote `Dashboard.razor`: kept the 4 summary cards (now backed by the single
  aggregate call), removed the table, added 3 `<canvas>` charts:
  1. Field-wide daily production trend — stacked area/line of oil/gas/water (30d).
  2. Production by artificial lift type — pie chart of total oil by lift type.
  3. Top wells by decline rate — horizontal bar chart of daily decline % per well.

Test results: backend 32/32 passed (16 Application.Tests + 16 Api.Tests... actually
15 Api.Tests + 16 Application.Tests = 31 total, incl. new KpiServiceDashboardSummaryTests).
Frontend: 17/17 passed. Both projects build clean (0 errors).

## 2026-08-25 — Fixed /dashboard 404 + loading UX polish

- Bug 1 root cause: `frontend/PetroVisorLite.Web/wwwroot/staticwebapp.config.json` was missing entirely, so Azure Static Web Apps had no `navigationFallback` rule and served a 404 for any direct/refresh request to a client-side route like `/dashboard`. Created the file with a `navigationFallback` rewriting to `/index.html`, excluding `_framework/*`, css/js/icons/appsettings, and standard static asset extensions (including `.dll`, `.wasm`, `.pdb`, `.blat`, `.dat` used by Blazor WASM) so framework assets are served directly instead of being rewritten. Verified it lands in `dotnet publish` output under `wwwroot/`.
- Bug 2: Confirmed Dashboard.razor already uses the single `/api/kpi/dashboard`-style aggregate call (`GetDashboardSummaryAsync`) instead of a per-well loop, and Program.cs has no blocking work before `RunAsync()` — the "gap" is normal WASM JIT/runtime warmup, not a fixable bug. Added a branded boot splash (matching app title/spinner) to `index.html`/`app.css` so the transition from 100% download to app render doesn't look like two different loading screens; left Dashboard's own quick "Loading..." state as-is since it's now effectively instant post-optimization.
- Validation: `dotnet build` (0 errors/warnings) and `dotnet test frontend/tests/PetroVisorLite.Web.Tests` — 17/17 passed. Also ran `dotnet publish -c Release` and confirmed `staticwebapp.config.json` appears in `publish/wwwroot/`.
- Not deployed per instructions — awaiting coordinated deploy step.
