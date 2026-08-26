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

## 2026-08-25 21:06 - Git init + initial commit
- Ran `git init` at repo root (was a folder-backed workspace with no .git).
- No global/local git identity existed; set local repo config: user.name "Jason Farrell", user.email "jasonfarrell@users.noreply.github.com".
- Rewrote .gitignore to cover: .NET bin/obj/publish/.vs/*.user (backend+frontend), TestResults/coverage, Node node_modules/dist/.env, IDE .vscode//.idea/, OS junk (.DS_Store/Thumbs.db), azd .azure/, local npm cache, and secret-pattern files (*secret*.json, *credential*.json, appsettings.*.local.json). Kept existing Squad-specific ignore rules.
- Scanned appsettings.json/appsettings.Development.json (backend+frontend) and .cs files for hardcoded secrets/connection strings/API keys: none found. Only a marked DEV-ONLY seed password constant and a masked comment example existed - both benign.
- Checked infra/rg-petrovisor-cus01.bicepparam: sqlAdministratorLoginPassword is a placeholder overridden at deploy time, no real secret.
- Staged all files with `git add -A` (469 files); verified no bin/obj/publish/node_modules leaked in and no file over ~140KB.
- Committed as 673268a: "Initial commit: PetroVisor Lite scaffold, Blazor frontend, KPI dashboard, Azure deployment infra".
- `git status` is clean. No remote configured/pushed per instructions (local-only).

## 2026-08-26 — CI/CD deploy plumbing (OIDC → Azure)

Wired GitHub Actions → Azure for `rg-petrovisor-cus01`. Executed (not just
documented) the Entra/RBAC setup:

- Created app registration + SP `sp-petrovisor-github-deploy`
  (appId `5260acff-ef5b-4c48-8bee-7fb94d859482`,
  SP object id `c5d7417a-b0b1-4705-87be-697295db4c70`).
  **No client secret** — `az ad app credential list` returns `[]`.
- One `Contributor` assignment, scoped to the resource group only:
  `/subscriptions/bb4b2781-.../resourceGroups/rg-petrovisor-cus01`.
- Three federated credentials for `jasonfarrell-msft/petrovisor-lite`:
  `ref:refs/heads/main`, `pull_request`, `environment:azure-prod`.
- Repo **variables** (not secrets): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
  `AZURE_SUBSCRIPTION_ID`.
- Created the bare `azure-prod` GitHub Environment.

Added `.github/workflows/deploy-infra.yml`: `validate` (bicep build) →
`what-if` (read-only, automatic on PR/push) and `deploy` (manual only).
Actions SHA-pinned; workflow perms default `contents: read`;
`id-token: write` only on the two jobs that log into Azure.

**Cost gate.** Wanted a GitHub Environment required-reviewer rule; the API
refused it — reviewer rules *and* wait timers are unavailable for private
repos on this billing plan. Moved the gate in-workflow instead of weakening
anything: deploy needs `workflow_dispatch` + `action=deploy` +
`confirm` typed as `rg-petrovisor-cus01` + `ref=main`. Push/PR can't satisfy
the first condition, so no merge can ever spend money. Simulated all 7
trigger scenarios — deploy is true in exactly one.

**Removed a secret rather than adding one.** I'd initially threaded a
`SQL_ADMIN_PASSWORD` secret through both deploy paths; then noticed
`modules/sql.bicep` sets `azureADOnlyAuthentication: true`, so no SQL
password exists or is needed. Dropped it, and deleted the
`sqlAdministratorLoginPassword = 'placeholder'` line from the bicepparam.
The workflow now consumes **zero** GitHub secrets.

**Brief was wrong about the RG being empty** — it holds 10 resources from
successful deployments on 2026-08-25 (a day before this session). I deployed
nothing; all timestamps predate my work. So the first CI `what-if` will show
a diff against live infra, not a green-field create. Also: `acrpetrovisor`
lives in the RG but isn't managed by `main.bicep`.

Verified: `actionlint` 1.7.7 exit 0, `az bicep build` + `build-params` exit 0,
and every Azure object re-queried after creation.

Files: `.github/workflows/deploy-infra.yml` (new), `infra/README.md`,
`infra/rg-petrovisor-cus01.bicepparam`,
`.squad/decisions/inbox/lando-oidc-cicd-deploy.md` (new). No commits made.

## 2026-08-26 — CD on main (user override of my cost gate)

The user explicitly overrode my previous design: *"basically any change to main
should start the full pipeline. Any PR should run a 'validation' pipeline."*
Removed the cost gate rather than defending it.

- `deploy` job now triggers on `push` to `main` (and dispatch with
  `action=deploy`). Deleted the `confirm` string input and the entire
  `confirmation-check` job.
- `what-if` job now triggers on `pull_request` (and dispatch with
  `action=what-if`). A PR reaches only this job — it has no `environment:` and
  never calls `deployment group create`, so a PR cannot mutate Azure.
- Kept `workflow_dispatch` with the `action` choice as a manual escape hatch;
  the `what-if` option is now the only way to preview against live infra
  without opening a PR, so it earned its keep.
- Added `az bicep build-params` to `validate` (fails a bad bicepparam in the
  cheap credential-free job). Added a `continue-on-error` what-if step inside
  the deploy job as an audit log only. Made `--mode Incremental` explicit.

**The OIDC question I was told to validate carefully, and the answer.** GitHub's
`sub` claim is decided by the *job*, not the trigger. A job with
`environment: azure-prod` gets `repo:.../...:environment:azure-prod`; the `ref:`
form is not used. So push-to-main hitting `deploy` still matches
`gh-petrovisor-env-azure-prod`. **No new federated credential was needed** —
all four paths (push→deploy, PR→what-if, dispatch-deploy, dispatch-what-if)
already map to an existing credential. Confirmed repo OIDC subject
customization is at `use_default: true`, so nothing rewrites the claim.
`environment: azure-prod` is load-bearing and stays.

Known, accepted gap: dispatch + what-if from a branch other than `main` would
present `ref:refs/heads/<branch>` and fail login. Fails closed at login — safe.

**Risk logged:** `acrpetrovisor` is in the RG but unmanaged by `main.bicep`.
Incremental mode leaves it alone, but auto-deploy means anyone later adding
`--mode Complete` would delete it *on merge* with no human in the loop — the
typed gate would previously have caught that. Documented in the workflow and
README that Complete mode is forbidden.

Verified: `actionlint` 1.7.7 exit 0; `az bicep build` + `build-params` clean
(only pre-existing `no-unused-params` warnings for the now-unused SQL admin
params); live `what-if` against the RG returned `status: Succeeded`.
Grep confirms the only `secrets.` match in the workflow is comment prose.

Rewrote the README's trigger semantics — the old "nothing deploys
automatically" text was actively wrong and would have misled the next person.

Committed **directly to `main`** per explicit user authorization (no feature
branch). Decision record: `.squad/decisions/inbox/lando-cd-on-main.md`
(gitignored, so intentionally not in the commit).
