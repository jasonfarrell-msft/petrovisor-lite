// PetroVisor Lite — top-level Azure infra scaffold.
// SCAFFOLD ONLY: not deployed. See infra/README.md.
//
// Deployment target: resource group `rg-petrovisor-cus01` in Central US.
// Create the RG first (out of band — see README), then deploy this template
// with `az deployment group create --resource-group rg-petrovisor-cus01 ...`.
//
// Provisions:
//   - User-Assigned Managed Identity for backend ACR image pull only
//     (backend runtime auth uses the Container App system-assigned identity)
//   - Azure Key Vault (RBAC-based, identity granted Secrets User role)
//   - Azure SQL (server + database), AAD admin configured, MI added as DB user out-of-band
//   - Log Analytics workspace (required by Container Apps environment)
//   - Container Apps environment + backend API container app
//   - Azure Static Web App (frontend — Blazor WebAssembly static build)
//
// All API versions below reflect this author's best knowledge of latest GA
// versions as of authoring time (2026-08-25) — REVERIFY against
// `az provider show` / Bicep types before any real deployment.

targetScope = 'resourceGroup'

@description('Azure region for most resources (RG, identity, Key Vault, SQL, Container Apps). Static Web Apps region is set independently — see staticWebAppLocation.')
param location string = 'centralus'

@description('Azure region for the Static Web App. Central US is a GA-supported Static Web Apps region, so this defaults to the same region as the rest of the RG. If that ever changes, fall back to a supported region such as westus2 or eastus2 and flag the deviation.')
param staticWebAppLocation string = 'centralus'

@description('Short, unique project name used to derive resource names.')
param projectName string = 'petrovisor'

@description('Region/deployment suffix used in CAF-style resource names (this deployment: "cus01" = Central US, instance 01).')
param nameSuffix string = 'cus01'

@description('Container image reference for the backend API, e.g. myregistry.azurecr.io/petrovisorlite-api:latest')
param backendImage string

@description('SKU for the Static Web App: "Standard" (custom auth/backends/private endpoints) or "Free" (cost-saving demo alternative).')
param staticWebAppSkuName string = 'Standard'

@description('SQL Server administrator login name (break-glass account only; day-to-day access should use AAD/Managed Identity).')
param sqlAdministratorLogin string = 'pvliteadmin'

@description('SQL Server administrator password. With AAD-only authentication, this is unused but retained for compatibility.')
@secure()
param sqlAdministratorLoginPassword string = ''

@description('Azure AD object ID of the user/group to set as SQL AAD admin.')
param sqlAadAdminObjectId string

@description('Azure AD login/display name of the SQL AAD admin.')
param sqlAadAdminLoginName string

var tenantId = subscription().tenantId

// CAF-style names, fixed for the rg-petrovisor-cus01 deployment target.
// (Kept parameterized above via projectName/nameSuffix in case this template
// is reused for another RG/region later — e.g. rg-petrovisor-eus01.)
var identityName = 'id-${projectName}-${nameSuffix}'
var keyVaultName = 'kv-${projectName}-${nameSuffix}' // 20 chars — within the 24-char KV name limit
var sqlServerName = 'sql-${projectName}-${nameSuffix}'
var sqlDatabaseName = '${projectName}db'
var containerAppsEnvName = 'cae-${projectName}-${nameSuffix}'
var backendAppName = 'ca-${projectName}-api-${nameSuffix}'
var staticWebAppName = 'stapp-${projectName}-web-${nameSuffix}'
var logAnalyticsName = 'law-${projectName}-${nameSuffix}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

module identity 'modules/identity.bicep' = {
  name: 'identity-deploy'
  params: {
    location: location
    identityName: identityName
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-deploy'
  params: {
    location: location
    keyVaultName: keyVaultName
    tenantId: tenantId
    principalId: identity.outputs.principalId
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql-deploy'
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    aadAdminLoginName: sqlAadAdminLoginName
    aadAdminObjectId: sqlAadAdminObjectId
    aadTenantId: tenantId
  }
}

module containerApps 'modules/containerapps.bicep' = {
  name: 'containerapps-deploy'
  params: {
    location: location
    environmentName: containerAppsEnvName
    logAnalyticsWorkspaceId: logAnalytics.id
    backendAppName: backendAppName
    backendImage: backendImage
    userAssignedIdentityId: identity.outputs.id
    keyVaultUri: keyVault.outputs.uri
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    sqlDatabaseName: sql.outputs.sqlDatabaseName
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deploy'
  params: {
    location: staticWebAppLocation
    staticWebAppName: staticWebAppName
    skuName: staticWebAppSkuName
    backendApiFqdn: containerApps.outputs.backendFqdn
  }
}

output managedIdentityClientId string = identity.outputs.clientId
output keyVaultUri string = keyVault.outputs.uri
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output backendUrl string = 'https://${containerApps.outputs.backendFqdn}'
output staticWebAppDefaultHostname string = staticWebApp.outputs.defaultHostname
output backendSystemAssignedPrincipalId string = containerApps.outputs.backendSystemAssignedPrincipalId
