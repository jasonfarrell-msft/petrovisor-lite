// Microsoft Foundry resource, project, and one model deployment.
// API versions: verify against the latest GA versions from `az provider show`
// before real deployment.

@description('Azure region for the Microsoft Foundry resource and project.')
param location string

@description('Globally unique name for the Microsoft Foundry resource.')
param foundryName string

@description('Object ID of the managed identity that will call the model.')
param principalId string

@description('Model name to deploy.')
param modelName string = 'gpt-5.4-mini'

@description('Version of the model to deploy.')
param modelVersion string = '2026-03-17'

@description('Deployment capacity in thousands of tokens per minute.')
param capacity int = 10

@description('Name clients use to address the model deployment.')
param deploymentName string = 'gpt-5.4-mini'

resource foundry 'Microsoft.CognitiveServices/accounts@2026-05-01' = {
  name: foundryName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    allowProjectManagement: true
    customSubDomainName: foundryName
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled' // NOTE: switch to 'Disabled' + Private Endpoint for production
  }
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2026-05-01' = {
  name: 'ask-petro-project'
  parent: foundry
  location: location
  properties: {
    displayName: 'ask-petro-project'
    description: 'Microsoft Foundry project for Ask PetroVisor.'
  }
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2026-05-01' = {
  name: deploymentName
  parent: foundry
  dependsOn: [
    project
  ]
  sku: {
    name: 'GlobalStandard'
    capacity: capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource cognitiveServicesOpenAIUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundry.id, principalId, cognitiveServicesOpenAIUserRoleId)
  scope: foundry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

output id string = foundry.id
output name string = foundry.name
output endpoint string = foundry.properties.endpoint
output deploymentName string = modelDeployment.name
