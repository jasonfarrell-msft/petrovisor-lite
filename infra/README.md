# PetroVisor Lite — Azure Infra Scaffold (Bicep)

**Status: deployed.** The resources below exist in `rg-petrovisor-cus01`
(first deployed 2026-08-25). CI/CD is wired via GitHub Actions using OIDC —
see [CI/CD deployment](#cicd-deployment-github-actions--oidc) below.

> ⚠️ **Continuous delivery is live.** Any push to `main` that touches
> `infra/**` (or the workflow file) deploys to `rg-petrovisor-cus01`
> automatically. Pull requests run validation + `what-if` only and never
> deploy.

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
- **Azure AI Foundry** account (`aif-petrovisor-cus01`) with an Ask PetroVisor
  model deployment. The backend Container App's system-assigned Managed Identity
  is granted access; no API keys or key-based auth are introduced.
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
| Azure AI Foundry account | `aif-petrovisor-cus01` | Ask PetroVisor model deployment; backend system-assigned MI access only |
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
    ├── aifoundry.bicep        # Azure AI Foundry account + model deployment
    ├── containerapps.bicep    # Container Apps environment + backend API app
    └── staticwebapp.bicep     # Static Web App (frontend, Blazor WASM)
```

## Azure AI Foundry for Ask PetroVisor

The `aifoundry` module provisions the Azure AI Foundry account
(`aif-petrovisor-cus01`), project, and model deployment used by Ask
PetroVisor. The top-level template wires the backend Container App's
**system-assigned** principal ID into the module so the backend authenticates
with Managed Identity only. Local/key-based authentication is disabled on the
account, and no model keys or secrets are emitted by this scaffold.

**Cost approval gate:** this resource and its model deployment are new Azure
spend, not reuse of existing infrastructure. Any deployment that creates or
changes the Foundry account/model capacity requires explicit user cost approval
tracked in the deployment task before applying.

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

## Manual deployment (local, for break-glass / debugging)

The resource group already exists — do not recreate it.

```bash
az login
az account set --subscription bb4b2781-6739-4fa1-994e-4ad6ce55c59c

# Read-only preview of what would change:
az deployment group what-if \
  --resource-group rg-petrovisor-cus01 \
  --template-file main.bicep \
  --parameters rg-petrovisor-cus01.bicepparam

# Actually apply:
az deployment group create \
  --resource-group rg-petrovisor-cus01 \
  --template-file main.bicep \
  --parameters rg-petrovisor-cus01.bicepparam
```

**No password parameter is required.** The SQL logical server is provisioned
with `azureADOnlyAuthentication: true`, so there is no SQL admin password to
supply, store, or rotate. Authentication is Entra ID only.

(`main.parameters.json` may be used in place of the `.bicepparam` file — both
target the same `rg-petrovisor-cus01` / `centralus` values.)

---

## CI/CD deployment (GitHub Actions + OIDC)

Workflow: [`.github/workflows/deploy-infra.yml`](../.github/workflows/deploy-infra.yml)

### Deploy identity

Deployments authenticate with **OpenID Connect / workload identity
federation**. There is **no client secret, no password, and no
`AZURE_CREDENTIALS` JSON blob** anywhere in this repository, in GitHub
secrets, or on the app registration. GitHub mints a short-lived OIDC token
per job and Entra ID exchanges it for an Azure access token.

| | |
|---|---|
| App registration / SP | `sp-petrovisor-github-deploy` |
| Application (client) ID | `5260acff-ef5b-4c48-8bee-7fb94d859482` |
| SP object ID | `c5d7417a-b0b1-4705-87be-697295db4c70` |
| Tenant ID | `e90bd921-0e00-4e6f-b87c-713670ee27bf` |
| Subscription | `bb4b2781-6739-4fa1-994e-4ad6ce55c59c` (`TSJasonFarrell-Sub`) |
| Client secrets | **none** (verify: `az ad app credential list --id 5260acff-ef5b-4c48-8bee-7fb94d859482` returns `[]`) |

### Role assignment scope

Exactly one role assignment, deliberately narrow:

- **Role:** `Contributor` (not Owner — the identity cannot grant RBAC or
  modify role assignments)
- **Scope:** `/subscriptions/bb4b2781-6739-4fa1-994e-4ad6ce55c59c/resourceGroups/rg-petrovisor-cus01`

Scope is the **resource group only**, never the subscription. The identity
cannot see or touch anything outside `rg-petrovisor-cus01`.

### Federated credentials

All three use issuer `https://token.actions.githubusercontent.com` and
audience `api://AzureADTokenExchange`, restricted to the repo
`jasonfarrell-msft/petrovisor-lite`:

| Name | Subject | Used by |
|---|---|---|
| `gh-petrovisor-env-azure-prod` | `repo:jasonfarrell-msft/petrovisor-lite:environment:azure-prod` | the `deploy` job (push to `main`, and dispatch with action=deploy) |
| `gh-petrovisor-pr` | `repo:jasonfarrell-msft/petrovisor-lite:pull_request` | the `what-if` job on pull requests |
| `gh-petrovisor-main` | `repo:jasonfarrell-msft/petrovisor-lite:ref:refs/heads/main` | the `what-if` job on a `workflow_dispatch` from `main` |

**Why the `environment:` credential covers push-to-main.** GitHub's OIDC
`sub` claim is determined by the *job*, not the trigger: when a job declares
`environment: azure-prod`, the subject becomes
`repo:<owner>/<repo>:environment:<name>` and the `ref:` form is **not** used.
So a push to `main` reaching the `deploy` job still presents
`...:environment:azure-prod` and matches `gh-petrovisor-env-azure-prod`.
This is why `environment: azure-prod` on the deploy job is load-bearing —
removing it would switch the subject to `ref:refs/heads/main` and change which
credential is exercised.

The `gh-petrovisor-main` credential remains useful for the manual
`workflow_dispatch` + `what-if` path, whose job has **no** `environment:` and
therefore presents `ref:refs/heads/main`.

Repository-level OIDC subject customization is at its default
(`use_default: true`), so no custom claim template alters these subjects.

### Repository variables (not secrets)

```
AZURE_CLIENT_ID        5260acff-ef5b-4c48-8bee-7fb94d859482
AZURE_TENANT_ID        e90bd921-0e00-4e6f-b87c-713670ee27bf
AZURE_SUBSCRIPTION_ID  bb4b2781-6739-4fa1-994e-4ad6ce55c59c
```

These are stored as **variables** (`gh variable set`), not secrets. None are
credentials — they are public identifiers that are useless without a
federated token from this specific repo. Storing them as variables keeps them
readable in logs and in the UI, which makes misconfiguration obvious instead
of silent. The actual security boundary is the federated credential subject,
not the confidentiality of these GUIDs.

**This workflow consumes zero GitHub secrets.**

### Trigger model — continuous delivery on `main`

**Any change merged/pushed to `main` that touches `infra/**` deploys
automatically.** Pull requests run a read-only validation pipeline and can
never deploy.

| Trigger | What runs | Touches Azure? |
|---|---|---|
| Pull request targeting `main` | `bicep build` + `build-params` + `what-if` | No — read-only |
| Push to `main` | `bicep build` + `build-params`, then `deployment group create` (incremental) | **Yes — deploys** |
| `workflow_dispatch`, action = `what-if` (default) | `bicep build` + `what-if` | No — read-only |
| `workflow_dispatch`, action = `deploy` | `bicep build`, then `deployment group create` | **Yes — deploys** |

Job guards:

- `what-if` job: `github.event_name == 'pull_request' || (workflow_dispatch && inputs.action == 'what-if')`
- `deploy` job: `github.event_name == 'push' || (workflow_dispatch && inputs.action == 'deploy')`

A `pull_request` event can never satisfy the `deploy` guard, so **a PR can
never change Azure.** The `push` trigger is already restricted to `main` by
`on.push.branches`, so no other branch deploys.

The deploy job runs a `what-if` first purely as an **audit log** — it is
`continue-on-error: true` and does not gate the deployment.

> **Previous behavior (superseded 2026-08-26).** This workflow used to require
> a manual `workflow_dispatch` with the operator typing `rg-petrovisor-cus01`
> into a `confirm` input, and pushes to `main` only ran `what-if`. That cost
> gate — along with the `confirm` input and the `confirmation-check` job — was
> removed at the user's explicit direction in favor of continuous delivery.

The deploy job targets the `azure-prod` GitHub Environment, which records
every deployment in the repo's environment history for audit — and, critically,
is what makes the OIDC subject match the federated credential (see below).

> **Note on environment protection rules.** A GitHub Environment
> *required-reviewer* rule on `azure-prod` is not available: required reviewers
> and wait timers are not offered for private repositories on this account's
> billing plan. **If this repo is made public or the plan is upgraded, adding a
> required reviewer to `azure-prod` would reintroduce a human approval step in
> front of the auto-deploy** without changing the workflow:
>
> ```bash
> gh api --method PUT repos/jasonfarrell-msft/petrovisor-lite/environments/azure-prod \
>   --input - <<< '{"reviewers":[{"type":"User","id":287220}],"prevent_self_review":true}'
> ```

### Deployment mode

Deployments use **incremental** mode (the `az` default, stated explicitly in
the workflow). `--mode Complete` must **never** be used: the resource group
contains resources that `main.bicep` does not manage (notably `acrpetrovisor`),
and Complete mode would delete them.

### Supply-chain hardening

- All third-party actions are pinned to a **full commit SHA**, with the
  human-readable tag in a trailing comment:
  - `actions/checkout@fbc6f3992d24b796d5a048ff273f7fcc4a7b6c09` (v5)
  - `azure/login@7184910d9eb2b1c5e48f7073824a90609bb9b6d6` (v2)
- Workflow-level `permissions:` default to `contents: read`.
- `id-token: write` is granted **per job**, only on the two jobs that call
  `azure/login`. The `bicep build` and confirmation jobs never receive it.
- `persist-credentials: false` on every checkout, so the `GITHUB_TOKEN` is not
  left in `.git/config` for later steps to pick up.
- `concurrency` with `cancel-in-progress: false` prevents two deployments from
  racing against the same resource group.

### Triggering a deployment

```bash
# Read-only preview (safe, no cost):
gh workflow run deploy-infra.yml -f action=what-if

# Real deployment (costs money — requires typed confirmation):
gh workflow run deploy-infra.yml \
  -f action=deploy \
  -f confirm=rg-petrovisor-cus01

gh run watch
```

Or from the GitHub UI: **Actions → Deploy Infra (Azure) → Run workflow**,
pick `deploy`, and type `rg-petrovisor-cus01` into the confirm box.

### Reproducing the identity setup

```bash
az ad app create --display-name sp-petrovisor-github-deploy --sign-in-audience AzureADMyOrg
az ad sp create --id <appId>
az role assignment create --assignee-object-id <spObjectId> \
  --assignee-principal-type ServicePrincipal --role Contributor \
  --scope /subscriptions/bb4b2781-6739-4fa1-994e-4ad6ce55c59c/resourceGroups/rg-petrovisor-cus01
az ad app federated-credential create --id <appId> --parameters '{
  "name":"gh-petrovisor-main",
  "issuer":"https://token.actions.githubusercontent.com",
  "subject":"repo:jasonfarrell-msft/petrovisor-lite:ref:refs/heads/main",
  "audiences":["api://AzureADTokenExchange"]}'
```

## Follow-ups for a future deployment pass

- Remove the now-unused `sqlAdministratorLogin` / `sqlAdministratorLoginPassword`
  parameters from `main.bicep` (they trigger `no-unused-params` linter warnings;
  the SQL module uses Entra-only auth and never consumes them).
- Add a required-reviewer protection rule to the `azure-prod` GitHub Environment
  if/when the billing plan supports it (see the CI/CD section).
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
