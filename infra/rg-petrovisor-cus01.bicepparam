// Parameters file targeting the rg-petrovisor-cus01 / Central US deployment.
// SCAFFOLD ONLY — not deployed. See ../README.md.
//
// Usage (the RG already exists):
//   az deployment group create \
//     --resource-group rg-petrovisor-cus01 \
//     --template-file main.bicep \
//     --parameters rg-petrovisor-cus01.bicepparam
//
// No password parameter is required: the SQL server is provisioned with
// azureADOnlyAuthentication = true (Entra ID authentication only).
using 'main.bicep'

param location = 'centralus'
param staticWebAppLocation = 'centralus'
param projectName = 'petrovisor'
param nameSuffix = 'cus01'
param backendImage = 'acrpetrovisor.azurecr.io/petrovisor-api:20260825-dashboard-charts'
param corsAllowedOrigins = 'https://lively-bay-0c1299310.7.azurestaticapps.net'
param seedDemoData = true
param staticWebAppSkuName = 'Standard'
param sqlAdministratorLogin = 'pvliteadmin'
param sqlAadAdminObjectId = '61a37498-9ab6-43d2-b70f-706fd58274e7'
param sqlAadAdminLoginName = 'jasonfarrell@MngEnvMCAP331427.onmicrosoft.com'
