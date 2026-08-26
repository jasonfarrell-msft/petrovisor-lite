# Leia — History

## Project Context
PetroVisor Lite — simplified upstream oil & gas data platform. ASP.NET Core Clean Architecture backend (Domain/Application/Infrastructure/API), EF Core + Azure SQL, ASP.NET Core Identity + JWT (Engineer/Viewer roles), React+TS frontend with Recharts/Chart.js, monorepo with /backend /frontend /infra (Bicep), xUnit + Jest/RTL tests, seed data via EF Core.

Requested by: jasonfarrell. Session start: 2026-08-25.

## 2026-08-25 — Backend Clean Architecture scaffold
Scaffolded top-level monorepo layout and `/backend` .NET 10 Clean Architecture
solution: `PetroVisorLite.Domain` (Well, ProductionRecord, Facility POCOs),
`PetroVisorLite.Application` (repository/service interfaces + example DTOs,
no implementations), `PetroVisorLite.Infrastructure` (EF Core SqlServer +
Identity package refs, skeleton `PetroVisorDbContext`), `PetroVisorLite.Api`
(ASP.NET Core Web API, Swashbuckle Swagger, `HealthController`, no hardcoded
connection strings — user-secrets/Key Vault noted), and
`PetroVisorLite.Application.Tests` (xUnit smoke test). Wrote top-level
`README.md` (architecture, setup, roadmap) and placeholder READMEs for
`/frontend` (Luke) and confirmed `/infra` (Lando) already scaffolded —
left untouched. Recorded architecture decision at
`.squad/decisions/inbox/leia-architecture.md`. Verified `dotnet build`
(0 errors/warnings) and `dotnet test` (1/1 passing) both succeed.

📌 Team update (2026-08-25T17:47:29Z): Luke updated the top-level README's frontend section to reflect the pivot to Razor Pages (`dotnet run` instead of `npm install`/`npm run dev`) — reconcile this if you do further README restructuring work. — decided by Luke
