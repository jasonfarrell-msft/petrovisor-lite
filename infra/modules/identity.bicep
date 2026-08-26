// Managed Identity module — User-Assigned Managed Identity used by both
// Container Apps (backend + frontend) to access Key Vault and SQL without
// embedded credentials.
// API version: verify against latest GA before real deployment.
param location string
param identityName string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

output id string = identity.id
output principalId string = identity.properties.principalId
output clientId string = identity.properties.clientId
