# PetroVisor Lite — Data Source Recommendation (Azure)

**Status: recommendation/documentation only — nothing here is implemented or deployed.**

## Current state (local/dev)

PetroVisor Lite already ships with a synthetic seed dataset via Han's EF Core
seeding service, which runs automatically in the `Development` environment:

- Roles + demo users
- 2 facilities
- 8 wells
- ~6 months of daily production records, with a decline-curve trend and a
  scripted artificial-lift status change partway through

## Recommendation for the Azure-deployed backing store

Use **Azure SQL Database** (`sql.bicep` in this scaffold, deployed as
`sql-petrovisor-cus01` / `petrovisordb`) as the production-parity backing
store for any Azure-deployed environment (dev/test/demo in Azure, not just
local). This keeps the same EF Core provider/migrations path as local
development — no branching database logic.

### Seeding the Azure-hosted database

Two options, in order of preference for a "lite" demo-oriented deployment:

1. **Re-run the same EF Core seeding service against Azure SQL**, guarded by
   the same idempotency check Han already implemented locally (skip seeding
   if data already exists). This is the simplest option and requires no new
   code — just ensuring the seeding service is invoked (e.g. by setting the
   app's environment to a mode that triggers seeding, or via an explicit
   admin/bootstrap endpoint) once against the Azure database.

2. **One-time post-deployment seeding step**, decoupled from the app's normal
   startup path — appropriate if seeding should never risk running against a
   populated production database. Options include:
   - An Azure Container Apps **Job** (one-shot, not a long-running app) that
     runs `dotnet ef database update` followed by the seed invocation.
   - A manual/pipeline step: `dotnet ef database update` against the Azure SQL
     connection string, then invoking the seed logic directly (CLI flag or a
     temporary admin trigger), run once by an operator.

Either way, do **not** auto-seed on every app startup in an Azure environment
that could contain real data — only in environments explicitly meant to hold
the synthetic demo dataset.

## What this scaffold does NOT do (yet)

- No container app job resource for seeding is defined in this scaffold.
- No migration/seed invocation is wired into any pipeline.
- This document is guidance for the next implementation pass, not a
  currently-provisioned resource.
