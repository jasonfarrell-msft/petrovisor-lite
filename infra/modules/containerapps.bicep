// Azure Container Apps environment + backend API container app.
// The frontend is now served by Azure Static Web Apps (see
// ../modules/staticwebapp.bicep) since the frontend is a Blazor WebAssembly
// static build, not a server-rendered/container workload — see
// infra/README.md and .squad/decisions/inbox/lando-cus01-deployment.md.
// API version: verify against latest GA before real deployment.
param location string
param environmentName string
param logAnalyticsWorkspaceId string

param backendAppName string
param backendImage string
param backendTargetPort int = 8080

@description('CORS allowed origins for the backend API (e.g. the Static Web App URL).')
param corsAllowedOrigins string = ''

@description('Whether to seed demo data on startup.')
param seedDemoData bool = false

param userAssignedIdentityId string
param userAssignedIdentityClientId string
param keyVaultUri string
param sqlServerFqdn string
param sqlDatabaseName string

@description('Azure AI Foundry account endpoint (resource identifier, not a secret — auth is via Managed Identity).')
param aiFoundryEndpoint string

@description('Azure AI Foundry model deployment name (resource identifier, not a secret).')
param aiFoundryDeploymentName string

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2022-10-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2022-10-01').primarySharedKey
      }
    }
  }
}

resource backendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: backendAppName
  location: location
  // SystemAssigned identity is used for all app-level runtime auth (SQL AAD auth,
  // Key Vault access via DefaultAzureCredential). The UserAssigned identity is
  // retained solely for ACR image pull (registry auth), keeping the two trust
  // boundaries separate.
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: backendTargetPort
        transport: 'auto'
      }
      registries: [
        {
          server: first(split(backendImage, '/'))
          identity: userAssignedIdentityId
        }
      ]
      // No secrets here: DB/Key Vault access is via the Managed Identity
      // configured above, referenced through env vars pointing at resource
      // names/URIs (not credentials).
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'backend'
          image: backendImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            // SQL access is granted to the user-assigned identity. Foundry uses
            // the system-assigned identity explicitly in application code.
            { name: 'AZURE_CLIENT_ID', value: userAssignedIdentityClientId }
            { name: 'KEYVAULT_URI', value: keyVaultUri }
            { name: 'SQL_SERVER_FQDN', value: sqlServerFqdn }
            { name: 'SQL_DATABASE_NAME', value: sqlDatabaseName }
            { name: 'AzureAiFoundry__Endpoint', value: aiFoundryEndpoint }
            { name: 'AzureAiFoundry__ModelName', value: aiFoundryDeploymentName }
            { name: 'AzureAiFoundry__DeploymentName', value: aiFoundryDeploymentName }
            { name: 'CORS_ALLOWED_ORIGINS', value: corsAllowedOrigins }
            { name: 'SEED_DEMO_DATA', value: seedDemoData ? 'true' : 'false' }
            { name: 'IMAGE_NAME', value: backendImage }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
    }
  }
}

output backendFqdn string = backendApp.properties.configuration.ingress.fqdn
output backendSystemAssignedPrincipalId string = backendApp.identity.principalId
