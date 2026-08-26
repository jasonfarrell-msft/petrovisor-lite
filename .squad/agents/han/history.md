# Han — History

## Project Context
PetroVisor Lite backend: ASP.NET Core Web API, Clean Architecture (Domain/Application/Infrastructure/API), EF Core targeting SQL Server/Azure SQL, ASP.NET Core Identity + JWT (Engineer/Viewer), CSV ingestion for well/production data, REST API for wells/production/KPIs, xUnit tests, seed data.

Requested by: jasonfarrell. Session start: 2026-08-25.

## 2026-08-25 — Fleshed out backend on Leia's scaffold

Built on Leia's Clean Architecture scaffold (did not re-scaffold):

- **Domain**: added `ArtificialLiftType`/`ArtificialLiftStatus` enums; extended
  `Well` (lift type/status) and `ProductionRecord` (choke size, wellhead
  pressure, per-record lift type/status). Facility/Well/ProductionRecord
  navigation properties fully wired.
- **Infrastructure**: `PetroVisorDbContext` now extends
  `IdentityDbContext<ApplicationUser, IdentityRole, string>` with full Fluent
  API config (keys, required fields, unique `Well.ApiNumber`, unique
  composite `(WellId, Date)` index). Added `ApplicationUser`, `Roles`
  constants, `WellRepository`/`FacilityRepository`/`ProductionRecordRepository`,
  `CsvImportService` (CsvHelper-based), `JwtTokenService`, `SeedData`
  (idempotent, Development-only), and `AddInfrastructureServices` DI
  extension. Added `dotnet-ef` local tool and generated the `InitialCreate`
  migration.
- **Api**: `AuthController` (login/register), `WellsController`,
  `FacilitiesController` (CRUD, Engineer-only writes), `ProductionController`
  (list by well + date range, CSV import), `KpiController` (thin wrapper
  around Obi-Wan's already-landed `IKpiService`/`KpiService` — no placeholder
  needed since his implementation existed by the time I wired it up).
  `Program.cs` now configures JWT bearer auth, Swagger with an Authorize
  button, and runs `Database.MigrateAsync()` + `SeedData.SeedAsync()` at
  startup (Development-gated, connection-string-gated).
- **Tests**: new `PetroVisorLite.Api.Tests` project — `CsvImportServiceTests`
  (5 tests, parsing/validation edge cases) and `AuthAndRoleEnforcementTests`
  (6 integration tests via `WebApplicationFactory` + EF Core InMemory
  provider, covering login and role-based 401/403/200/201 enforcement).
  `dotnet test`: 26/26 passing (15 existing + 11 new).
- Added a dev-only `docker-compose.yml` at the repo root for a local SQL
  Server container (not part of the app's own container topology).
- Verified: `dotnet build` clean (0 warnings/errors, 7 projects),
  `dotnet ef migrations add InitialCreate` succeeded, `dotnet run` starts the
  host and `GET /api/health` responds 200. Could **not** verify against a
  real SQL Server (no LocalDB on macOS, Docker daemon not running in this
  sandbox) — flagged in the decision doc for whoever has LocalDB/Docker to
  confirm `dotnet ef database update` + seeding end-to-end.
- Decision recorded at `.squad/decisions/inbox/han-backend.md`.

## 2026-08-25: Config-driven, idempotent seeding mechanism

Decoupled seeding from `ASPNETCORE_ENVIRONMENT`. Added `Seeding:Enabled` config key
(`appsettings.json` → `false`, `appsettings.Development.json` → `true`), plus a
`SEED_DEMO_DATA` env var override read directly in `Program.cs` (wins over the config key
when set) for easy toggling in Container Apps without touching appsettings. Wired the flag
check into `Program.cs` right after `dbContext.Database.MigrateAsync()`, replacing the old
`app.Environment.IsDevelopment()` gate. `SeedData.SeedAsync` idempotency check moved from
`Facilities.AnyAsync()` to `Wells.AnyAsync()` (more central/specific table) — unchanged
otherwise, still safe to call every startup. Added TODO comments + expected method signatures
in `SeedData.cs` for wiring in Obi-Wan's `RealisticSeedDataGenerator` once it lands (it did not
exist yet as of this work). Added `SeedingTests.cs` (4 new tests: seeds when enabled+empty,
skips when disabled, env var override wins, SeedAsync called twice doesn't duplicate rows).
Full suite: 30/30 passing (15 Application.Tests + 15 Api.Tests), `dotnet build` clean.

## 2026-08-25 (later): Wired in Obi-Wan's RealisticSeedDataGenerator

`RealisticSeedDataGenerator.cs` landed at
`backend/src/PetroVisorLite.Infrastructure/Seeding/RealisticSeedDataGenerator.cs`. It exposes a
single `Generate(DateOnly? asOf = null)` static method returning a
`GeneratedData(Facilities, Wells, Records)` record — not the three separate
`GenerateFacilities`/`GenerateWells`/`GenerateProductionRecords` methods I'd stubbed as a guess.
Replaced the hand-rolled facility/well/production-record generation block in `SeedData.SeedAsync`
with a single `RealisticSeedDataGenerator.Generate()` call, added the
`PetroVisorLite.Infrastructure.Seeding` using, and updated the class doc comment. No signature
mismatches beyond the method shape — the generator has no EF/DbContext dependency, so
`dbContext.Facilities/Wells/ProductionRecords.AddRange(...)` + `SaveChangesAsync()` still just
work as before. `dotnet build`: 0 warnings/errors. `dotnet test`: 30/30 passing (15 Application +
15 Api, including my 4 seeding tests which now exercise the real generator end-to-end via EF
InMemory). Ad-hoc smoke check (scratch console project, removed after): generator produces
**3 facilities, 15 wells, 18,286 production records** with no exceptions.
