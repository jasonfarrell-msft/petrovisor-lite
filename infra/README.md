# PetroVisor Lite — Azure Infra Scaffold (Bicep)

**Status: scaffolding only. Nothing here has been deployed.**

## Deployment target

- **Resource group:** `rg-petrovisor-cus01`
- **Region:** Central US (`centralus`) for all resources, **except** the
  Static Web App — see the regional caveat below.

This folder provisions the Azure footprint for PetroVisor Lite:

- User-Assigned Managed Identity (used by the backend Container App)
- Azure Key Vault (RBAC-authorization; the Managed Identity is granted the
  built-in **Key Vault Secrets User** role — no access policies, no keys/connection
  strings baked into app config)
- Azure SQL (logical server + single database), with an Azure AD admin configured;
  day-to-day app access is intended to go through the Managed Identity (added as a
  contained database user via `CREATE USER [<mi-name>] FROM EXTERNAL PROVIDER;`,
  run once out-of-band/via migration — not part of this Bicep)
- Log Analytics workspace (required by the Container Apps environment for logs)
- Azure Container Apps environment + backend API Container App (`ca-petrovisor-api-cus01`)
- **Azure Static Web App** (`stapp-petrovisor-web-cus01`) — hosts the Blazor
  WebAssembly frontend as a static build (replaces the earlier frontend
  Container App design now that the frontend is Blazor WASM, not a
  server-rendered app)

## Resource list (CAF-style naming)

| Resource | Name | Notes |
|---|---|---|
| Resource group | `rg-petrovisor-cus01` | Central US |
| Managed Identity | `id-petrovisor-cus01` | Attached to the backend Container App |
| Key Vault | `kv-petrovisor-cus01` | 20 chars — fits the 24-char KV name limit |
| SQL logical server | `sql-petrovisor-cus01` | |
| SQL database | `petrovisordb` | |
| Log Analytics workspace | `law-petrovisor-cus01` | |
| Container Apps environment | `cae-petrovisor-cus01` | |
| Container App (backend API) | `ca-petrovisor-api-cus01` | Image parameterized via `backendImage` |
| Static Web App (frontend) | `stapp-petrovisor-web-cus01` | Blazor WASM static build |

## Layout

```
infra/
├── main.bicep                        # top-level orchestration (RG-scoped)
├── main.parameters.json              # example parameters (no real secrets)
├── rg-petrovisor-cus01.bicepparam    # concrete params for this deployment target
├── DATA-SOURCES.md                   # backing data store recommendation
└── modules/
    ├── identity.bicep         # User-Assigned Managed Identity
    ├── keyvault.bicep         # Key Vault + RBAC role assignment
    ├── sql.bicep              # SQL server + database + firewall rule
    ├── containerapps.bicep    # Container Apps environment + backend API app
    └── staticwebapp.bicep     # Static Web App (frontend, Blazor WASM)
```

## Static Web Apps regional note

Azure Static Web Apps is only available in a specific subset of Azure regions
(distinct from the broader region list for most resources — the actual static
content is served from Azure Front Door's global edge network regardless of
the "region" set on the `Microsoft.Web/staticSites` resource, which mainly
affects the resource's control-plane/build location). **Central US is a
GA-supported Static Web Apps region**, so this scaffold uses `centralus` for
the Static Web App too — no deviation was needed for this deployment. This is
still tracked as a separate `staticWebAppLocation` parameter (defaulting to
`centralus`) in case a future deployment needs a different region; if so,
fall back to another GA-supported region such as `westus2` or `eastus2` and
update this note accordingly.

## Frontend build output path (needs reconciliation with Luke)

At authoring time, Luke (frontend agent) is converting the frontend from
Razor Pages to Blazor WebAssembly specifically to produce a static output
compatible with Static Web Apps. No decision record for that pivot was found
yet at `.squad/decisions/inbox/luke-blazor-wasm-pivot.md` or in
`.squad/decisions.md`.

**Best-guess publish path used for planning purposes (needs reconciliation
once Luke's work lands):**
```
frontend/PetroVisorLite.Web/bin/Release/net<version>/publish/wwwroot
```

The Static Web App's `buildProperties.outputLocation` in
`modules/staticwebapp.bicep` defaults to `wwwroot`, consistent with a
standard Blazor WASM publish layout. Confirm the actual project name/target
framework once Luke's pivot is committed, and update `appLocation`/
`appArtifactLocation` params accordingly.

## Wiring the backend API URL into the frontend

The Static Web App module accepts `backendApiFqdn` (wired from the
`containerapps` module's output) and provisions it as a
`Microsoft.Web/staticSites/config` `appsettings` entry
(`BACKEND_API_BASE_URL`). **Important deployment-time caveat:** Static Web
Apps app settings are only exposed to a *linked Azure Functions API*, not
directly readable by static client-side files. Since the Blazor WASM app has
no linked Functions API in this scaffold, the practical way to get the
backend URL into the client at deploy time is one of:

- Overriding `wwwroot/appsettings.json` (or an environment-specific
  `appsettings.{Environment}.json`) as a post-build step before the SWA
  deploy, injecting the Container App's FQDN.
- Fetching a small runtime config file (e.g. `config.json`) generated at
  deploy time and read by the Blazor app's startup code.
- Using the SWA CLI's `--app-settings`/pipeline environment variables to
  templating a config file during the build step, rather than expecting the
  Bicep-provisioned app setting to reach the client directly.

This is documented here as a deployment-time consideration; it is not wired
into any pipeline by this scaffold.

## API versions used

Chosen to reflect the latest **GA** API version known at authoring time
(2026-08-25). **Re-verify these before any real deployment** — Azure ships new
API versions frequently and some may have been superseded:

| Resource | API version |
|---|---|
| `Microsoft.ManagedIdentity/userAssignedIdentities` | `2023-01-31` |
| `Microsoft.KeyVault/vaults` | `2023-07-01` |
| `Microsoft.Authorization/roleAssignments` | `2022-04-01` |
| `Microsoft.Sql/servers`, `.../databases`, `.../firewallRules` | `2023-08-01-preview` (pin to a confirmed GA version, e.g. `2021-11-01`, if avoiding preview APIs is required) |
| `Microsoft.App/managedEnvironments`, `Microsoft.App/containerApps` | `2024-03-01` |
| `Microsoft.OperationalInsights/workspaces` | `2023-09-01` |
| `Microsoft.Web/staticSites`, `.../staticSites/config` | `2023-12-01` |

Verify current versions with:
```bash
az provider show --namespace Microsoft.App --query "resourceTypes[?resourceType=='containerApps'].apiVersions" -o tsv
az provider show --namespace Microsoft.Web --query "resourceTypes[?resourceType=='staticSites'].apiVersions" -o tsv
```

## Why Container Apps (backend) + Static Web Apps (frontend)

- The **backend** deployment unit is already a Docker image
  (`backend/Dockerfile`) — Container Apps is a native fit, with scale-to-zero
  and a shared managed environment (with Log Analytics) suited to a "lite"
  project.
- The **frontend** is being converted to Blazor WebAssembly (Luke, in
  parallel), which produces a static-file build with no server runtime
  needed. Static Web Apps is purpose-built for exactly this (static hosting +
  global CDN + optional linked Functions API), and is generally
  cheaper/simpler than running a container app just to serve static files.
  This replaces the earlier design (both backend and frontend as Container
  Apps) now that the frontend's actual output shape is known.

## Managed Identity design (no embedded credentials)

- A single User-Assigned Managed Identity is created and attached to the
  backend Container App (`identity.type: UserAssigned`). The Static Web App
  does not use this identity — it has no server-side compute needing SQL/Key
  Vault access in this scaffold.
- The identity is granted the **Key Vault Secrets User** RBAC role on the Key Vault
  — the app reads secrets via the Key Vault SDK using `DefaultAzureCredential`,
  never via a Key Vault connection string.
- SQL access should use Azure AD authentication with the Managed Identity as a
  contained database user (`CREATE USER ... FROM EXTERNAL PROVIDER`), avoiding
  SQL login/password connection strings for the application at runtime.
- The SQL Server's own admin login/password (`sqlAdministratorLogin` /
  `sqlAdministratorLoginPassword`) is a required break-glass account for the
  logical server resource itself; it is marked `@secure()` in Bicep and **must**
  be supplied at deploy time (e.g. from Key Vault or a secure pipeline variable),
  never committed to source. `main.parameters.json` ships a placeholder value only.

## Data source recommendation

See [`DATA-SOURCES.md`](./DATA-SOURCES.md) for the recommended backing data
store for an Azure-deployed environment: **Azure SQL Database** (already the
IaC target here), with the existing local EF Core seeding service either
re-run idempotently against Azure SQL, or invoked as a one-time
post-deployment step (e.g. a Container Apps Job) rather than auto-seeding on
every startup.

## Prerequisites (for when this is actually deployed — not yet)

```bash
az login
az account set --subscription <subscription-id>
az group create --name rg-petrovisor-cus01 --location centralus
```

## Example deployment command (NOT run by this scaffold)

```bash
az deployment group create \
  --resource-group rg-petrovisor-cus01 \
  --template-file main.bicep \
  --parameters rg-petrovisor-cus01.bicepparam \
  --parameters sqlAdministratorLoginPassword="$(az keyvault secret show \
      --vault-name <bootstrap-kv> --name sql-admin-password --query value -o tsv)" \
  --parameters backendImage=<acr>.azurecr.io/petrovisorlite-api:<tag>
```

(Or use `main.parameters.json` in place of the `.bicepparam` file — both are
provided, targeting the same `rg-petrovisor-cus01` / `centralus` values.)

## Follow-ups for a future deployment pass

- Confirm exact GA API versions (see table above) against current Azure docs.
- Reconcile the Blazor WASM publish path once Luke's Razor→WASM pivot lands
  (`.squad/decisions/inbox/luke-blazor-wasm-pivot.md` or `.squad/decisions.md`).
- Decide whether public network access on SQL/Key Vault should be disabled in
  favor of Private Endpoints + VNet integration (recommended for production;
  left `Enabled` here to keep the scaffold deployable without a VNet).
- Wire an Azure Container Registry (ACR) + `AcrPull` role assignment for the
  Managed Identity if pulling from a private registry.
- Add the out-of-band SQL migration/script that creates the contained DB user
  for the Managed Identity.
- Decide/implement the actual SWA deploy mechanism (SWA CLI vs. GitHub Actions
  integration) and how the backend API URL config override is templated in.
- Consider a Container Apps Job for one-time database seeding in Azure — see
  `DATA-SOURCES.md`.
