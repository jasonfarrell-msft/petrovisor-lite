// Azure SQL module: logical server + single database.
// Auth: Azure AD (Entra ID) admin set to the deploying identity/group;
// the app's User-Assigned Managed Identity is added as an AAD user inside the
// database (via a post-deploy script/migration) rather than via SQL auth —
// this avoids storing a connection string with embedded credentials.
// An administratorLoginPassword param is still accepted for the required
// break-glass SQL admin account, sourced from Key Vault / secure prompt only.
// API version: verify against latest GA before real deployment.
param location string
param sqlServerName string
param sqlDatabaseName string
param aadAdminLoginName string
param aadAdminObjectId string
param aadTenantId string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: aadAdminLoginName
      sid: aadAdminObjectId
      tenantId: aadTenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled' // required by organizational policy; use private endpoints
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'GP_S_Gen5_1' // General Purpose Serverless, Gen5, 1 vCore — cheap default for scaffold/dev
    tier: 'GeneralPurpose'
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
