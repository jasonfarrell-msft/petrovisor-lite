# Lando — History

## Project Context
PetroVisor Lite infra: Dockerfiles for /backend and /frontend, Bicep IaC in /infra for Azure App Service or Container Apps (current API versions), Managed Identity + Key Vault-ready config, no deployment yet.

Requested by: jasonfarrell. Session start: 2026-08-25.

## 2026-08-25 — Initial IaC & container scaffold
- `backend` and `frontend` did not exist yet when I started; wrote Dockerfiles
  speculatively per documented conventions (backend API project at
  `backend/src/PetroVisorLite.Api/PetroVisorLite.Api.csproj`, .NET LTS
  multi-stage build; frontend Vite/React + nginx multi-stage build with SPA
  fallback `nginx.conf`).
- Created `infra/` Bicep scaffold: `main.bicep` orchestrating
  `modules/identity.bicep`, `modules/keyvault.bicep`, `modules/sql.bicep`,
  `modules/containerapps.bicep`; `main.parameters.json` (placeholder-only
  secure param, no real secrets); `README.md` documenting prerequisites,
  API versions used (flagged for reverification), and the Managed
  Identity + Key Vault design (RBAC Key Vault Secrets User role, AAD-based
  SQL access intended via Managed Identity as contained DB user).
- Chose Azure Container Apps over App Service (Dockerfiles are the deployment
  unit; scale-to-zero fits a "lite" project).
- No deployment executed; no application code touched.
- Full rationale recorded in `.squad/decisions/inbox/lando-infra.md`.

## 2026-08-25 — Concrete deployment target: rg-petrovisor-cus01 (Central US)

Set up a concrete (still undeployed) deployment target per request:

- RG `rg-petrovisor-cus01` / `centralus`, resource-group-scoped `main.bicep`
  (already was RG-scoped; fixed CAF-style names for this target: `id-`, `kv-`,
  `sql-`, `cae-`, `ca-petrovisor-api-cus01`, `stapp-petrovisor-web-cus01`,
  `law-`).
- New `infra/modules/staticwebapp.bicep` (Microsoft.Web/staticSites@2023-12-01)
  for the Blazor WASM frontend — replaces the earlier frontend Container App.
  Central US confirmed as a GA-supported Static Web Apps region, so no
  regional deviation needed (documented anyway via a separate
  `staticWebAppLocation` param for future flexibility).
- Backend Container App retained/updated (`ca-petrovisor-api-cus01`), image
  still parameterized (`backendImage`), MI + Key Vault + SQL wiring unchanged.
- New `infra/rg-petrovisor-cus01.bicepparam`; updated `main.parameters.json`.
- Rewrote `infra/README.md` (RG/region, resource list, SWA regional caveat,
  prerequisites, backend-URL-into-frontend deployment-time caveat) and added
  `infra/DATA-SOURCES.md` recommending Azure SQL as the production-parity
  backing store, with Han's seeding service either re-run idempotently or
  invoked as a one-time post-deploy step.
- Luke's Blazor WASM pivot decision record wasn't found yet at task time —
  used the conventional publish path as a best guess and flagged it for
  reconciliation in the README.
- Recorded full decision at `.squad/decisions/inbox/lando-cus01-deployment.md`.
- Still nothing deployed — scaffolding/authoring only, per standing constraint.
