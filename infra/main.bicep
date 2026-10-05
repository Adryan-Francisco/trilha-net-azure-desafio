@description('Prefixo curto, sem espaços, para os recursos.')
@minLength(3)
@maxLength(11)
param prefix string = 'rhdesafio'
param location string = resourceGroup().location
param sqlAdmin string = 'rhadmin'
@secure()
param sqlPassword string

var suffix = uniqueString(resourceGroup().id)
var storageName = '${prefix}${suffix}'
var appName = '${prefix}-${suffix}'
var sqlName = '${prefix}-sql-${suffix}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storage
  name: 'default'
}
resource table 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'FuncionarioLog'
}
resource sql 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlName
  location: location
  properties: {
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
  }
}
resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sql
  name: 'RH'
  location: location
  sku: { name: 'Basic', tier: 'Basic', capacity: 5 }
}
resource firewall 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sql
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${prefix}-plan'
  location: location
  kind: 'linux'
  sku: { name: 'B1', tier: 'Basic', capacity: 1 }
  properties: { reserved: true }
}
resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__ConexaoPadrao', value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Initial Catalog=${database.name};User ID=${sqlAdmin};Password=${sqlPassword};Encrypt=True;TrustServerCertificate=False;' }
        { name: 'ConnectionStrings__SAConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'ConnectionStrings__AzureTableName', value: table.name }
      ]
    }
  }
}
output appName string = app.name
output sqlServerName string = sql.name
output storageAccountName string = storage.name
output swaggerUrl string = 'https://${app.properties.defaultHostName}/swagger'
