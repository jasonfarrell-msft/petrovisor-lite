# PetroVisor Lite

A simplified upstream oil & gas data platform: well/production data ingestion,
KPI analytics, and dashboards. Built as a foundation for later AI-agent
iteration and eventual Azure deployment.

## Architecture

Clean Architecture backend (`/backend`) with a strict dependency direction:

```
PetroVisorLite.Domain          <- no dependencies (entities: Well, ProductionRecord, Facility)
        ^
PetroVisorLite.Application     <- depends on Domain (interfaces, DTOs — no implementations)
        ^
PetroVisorLite.Infrastructure  <- depends on Application + Domain (EF Core, Identity)
        ^
PetroVisorLite.Api             <- depends on Infrastructure + Application (ASP.NET Core Web API)
```

- **Domain** — plain POCO entities, no EF Core or other framework dependencies.
- **Application** — repository/service interfaces (`IWellRepository`,
  `IProductionRecordRepository`, `IKpiService`, `ICsvImportService`) and DTOs.
  Obi-Wan's KPI logic and Han's use-case orchestration build on this layer.
- **Infrastructure** — EF Core `PetroVisorDbContext` (skeleton) and ASP.NET Core
  Identity package references. Han implements repositories and DbContext
  configuration here.
- **Api** — ASP.NET Core Web API host, Swagger/OpenAPI, and controllers.
  Currently only a `HealthController` proving the host builds and runs.

See `.squad/decisions/inbox/leia-architecture.md` for the full architecture
decision record.

## Repo layout

```
/backend    .NET Clean Architecture solution (this round: Leia)
/frontend   Blazor WebAssembly (standalone) app (owned by Luke)
/infra      Azure Bicep IaC (owned by Lando)
```

## Backend — getting started

Requires the .NET SDK (developed against .NET 10; see
`backend/global.json` if pinned).

```bash
cd backend
dotnet restore
dotnet build
dotnet test

# Run the API (Swagger UI at /swagger in Development)
dotnet run --project src/PetroVisorLite.Api
```

### Configuration & secrets

`appsettings.json` ships an **empty** `ConnectionStrings:PetroVisorDb` —
no credentials are committed. Locally, set it via user-secrets:

```bash
cd backend/src/PetroVisorLite.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PetroVisorDb" "<your-local-connection-string>"
```

In Azure, the connection string (or, preferably, just the SQL server name —
using Azure AD auth) should be sourced via **Managed Identity** and **Azure
Key Vault**, never hardcoded or stored as a plain app setting.

### Migrations & seeding (once Han adds them)

EF Core packages are referenced in `PetroVisorLite.Infrastructure`, but no
migrations exist yet — `PetroVisorDbContext` is currently a skeleton with
just `DbSet`s. Once Han adds entity configuration and an initial migration:

```bash
dotnet ef migrations add InitialCreate --project src/PetroVisorLite.Infrastructure --startup-project src/PetroVisorLite.Api
dotnet ef database update --project src/PetroVisorLite.Infrastructure --startup-project src/PetroVisorLite.Api
```

## Frontend — getting started

`/frontend/PetroVisorLite.Web` is a **Blazor WebAssembly (standalone)** app —
its **second** pivot (React -> ASP.NET Core Razor Pages -> Blazor WASM). It
moved off Razor Pages because the target host, **Azure Static Web Apps**,
only serves static files (HTML/JS/wasm) plus an optional Functions API — it
cannot run a persistent Kestrel/ASP.NET Core server process, which Razor
Pages requires. Blazor WASM compiles to static output that Static Web Apps
can host natively while staying a .NET/C# frontend. See
`.squad/decisions/inbox/luke-blazor-wasm-pivot.md` (this pivot) and
`.squad/decisions/inbox/luke-frontend-pivot.md` (the earlier React -> Razor
Pages pivot, forced by this environment's restricted npm registry access —
that abandoned React attempt is kept for reference in `/old-frontend`). Only
NuGet packages and CDN `<script>`/`<link>` tags (Bootstrap, Chart.js) are
used — no npm/node build step at all.

```bash
cd frontend
dotnet restore
dotnet build
dotnet test

# Run the Blazor WASM dev server (Login page at /login)
dotnet run --project PetroVisorLite.Web

# Publish static output for Azure Static Web Apps
dotnet publish PetroVisorLite.Web -c Release
# -> PetroVisorLite.Web/bin/Release/net10.0/publish/wwwroot
```

### Configuration

The backend API base URL is read from `wwwroot/appsettings.json` (and
`appsettings.Development.json`) — `BackendApi:BaseUrl` — never hardcoded.
Since Blazor WASM runs entirely in the browser with no server-side
environment at runtime, this is the only place to configure it; point it at
wherever `PetroVisorLite.Api` is running (default local dev:
`https://localhost:5001/`).

### Auth

Login posts credentials to the backend's `/api/auth/login` via `HttpClient`;
the returned JWT is stored in browser `localStorage` (via
`Blazored.LocalStorage`) and decoded client-side (no signature validation
needed here — the backend validates the signature on every API call) by a
custom `AuthenticationStateProvider` that drives Blazor's `<AuthorizeView>`/
`[Authorize]` component-level authorization. A `DelegatingHandler` attaches
the stored JWT as a bearer token on every outgoing API call automatically.
`[Authorize(Roles = "Engineer")]` gates the CSV Import page; Viewer gets
read-only pages.

## Roadmap — what a "full PetroVisor" would add next

This "Lite" foundation intentionally omits scope that a production upstream
data platform would eventually need:

- **ML/AI analytics** — decline curve forecasting, anomaly detection on
  production streams, predictive maintenance for artificial lift equipment.
- **ESG tracking** — emissions (methane, flaring/venting), water usage/disposal
  reporting, and regulatory ESG disclosures.
- **Artificial lift optimization** — rod pump, ESP, gas lift performance
  monitoring and optimization recommendations.
- **Multi-tenant support** — organization/tenant isolation, row-level security,
  per-tenant configuration and branding.
- **Real-time ingestion** — streaming well/SCADA data via Azure IoT Hub and
  Event Hubs instead of batch CSV import.
- Reservoir engineering data (completions, well tests, decline analysis).
- Role-based access control beyond basic ASP.NET Core Identity.
- Full observability (Application Insights, distributed tracing) and
  production-grade Azure infrastructure (see `/infra`, owned by Lando).
