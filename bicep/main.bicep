// Main Bicep template for Expense Management System
// Deploys App Service, Managed Identity, and Azure SQL
// Optionally deploys Azure OpenAI and Cognitive Search for chat functionality

targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Azure AD Object ID of the SQL admin')
param adminObjectId string

@description('Azure AD login name of the SQL admin')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI and Cognitive Search)')
param deployGenAI bool = false

// Variables for resource naming (all lowercase)
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var managedIdentityName = 'mid-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var databaseName = 'Northwind'

// Deploy App Service with User-Assigned Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
    managedIdentityName: managedIdentityName
  }
}

// Deploy Azure SQL with Entra ID authentication only
module azureSQL 'azure-sql.bicep' = {
  name: 'azureSQLDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: databaseName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    managedIdentityName: managedIdentityName
  }
}

// Conditionally deploy GenAI resources
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: 'swedencentral' // GPT-4o availability
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = managedIdentityName
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output databaseName string = databaseName

// Conditional GenAI outputs (use safe navigation)
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
