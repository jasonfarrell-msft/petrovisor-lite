// Parameters file targeting the rg-petrovisor-cus01 / Central US deployment.
// SCAFFOLD ONLY — not deployed. See ../README.md.
//
// Usage (once the RG exists — see README prerequisites):
//   az deployment group create \
//     --resource-group rg-petrovisor-cus01 \
//     --template-file main.bicep \
//     --parameters rg-petrovisor-cus01.bicepparam \
//     --parameters sqlAdministratorLoginPassword=<from-secure-store>
using 'main.bicep'

param location = 'centralus'
param staticWebAppLocation = 'centralus'
param projectName = 'petrovisor'
param nameSuffix = 'cus01'
param backendImage = 'acrpetrovisor.azurecr.io/petrovisor-api:latest'
param staticWebAppSkuName = 'Standard'
param sqlAdministratorLogin = 'pvliteadmin'
param sqlAdministratorLoginPassword = 'placeholder' // overridden at deploy time
param sqlAadAdminObjectId = '61a37498-9ab6-43d2-b70f-706fd58274e7'
param sqlAadAdminLoginName = 'jasonfarrell@MngEnvMCAP331427.onmicrosoft.com'
