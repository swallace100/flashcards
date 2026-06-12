// Flashcards app — Azure infrastructure
//
// Provisions:
//   - Azure SQL Server (Entra ID-only auth) + Basic DTU database
//   - App Service Plan (Linux F1 free) + App Service (.NET 10) with Managed Identity
//   - Azure Static Web App (free tier)
//
// Post-deployment steps (cannot be automated in Bicep):
//   1. Run database/schema.sql against the new database
//   2. Grant the App Service Managed Identity access to the database:
//        CREATE USER [<appServiceName>] FROM EXTERNAL PROVIDER;
//        ALTER ROLE db_datareader ADD MEMBER [<appServiceName>];
//        ALTER ROLE db_datawriter ADD MEMBER [<appServiceName>];
//   3. Link the Static Web App to your GitHub repo in the Azure portal
//   4. Re-deploy with the Static Web App URL as the frontendOrigin parameter

@description('Azure region for SQL, App Service, and App Service Plan')
param location string = resourceGroup().location

@description('Azure region for the Static Web App (must be a supported SWA region)')
param staticWebAppLocation string = 'eastasia'

@description('Name for the Azure SQL Server (must be globally unique)')
param sqlServerName string = 'flashcards-server'

@description('Name for the Azure SQL Database')
param databaseName string = 'flashcards-db'

@description('Name for the App Service and its plan')
param appServiceName string = 'flashcards-backend'

@description('Name for the Static Web App')
param staticWebAppName string = 'flashcards-frontend'

@description('Entra ID admin email — used to set the SQL Server AAD administrator')
param entraAdminLogin string

@description('Entra ID admin object ID — find this in Azure Portal > Microsoft Entra ID > Users')
param entraAdminObjectId string

@description('Frontend origin URL for CORS (the Static Web App URL). Leave empty on first deploy, then re-deploy with the output value.')
param frontendOrigin string = ''

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: entraAdminLogin
      sid: entraAdminObjectId
      tenantId: tenant().tenantId
      principalType: 'User'
    }
  }
}

// Allow connections from other Azure services (required for App Service → SQL)
resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Basic DTU tier — ~$5/month flat, 5 DTUs, 2GB storage, always on
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

// ── App Service ───────────────────────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${appServiceName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true // required for Linux
  }
}

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned' // Managed Identity — authenticates to SQL without a password
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      appSettings: [
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Authentication=Active Directory Managed Identity;Encrypt=True'
        }
        {
          name: 'AllowedOrigin'
          value: frontendOrigin
        }
      ]
    }
  }
}

// ── Static Web App ────────────────────────────────────────────────────────────

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: staticWebAppLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Use this as the frontendOrigin parameter on re-deploy')
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'

@description('Use this as the app name when granting Managed Identity SQL access')
output appServiceName string = appService.name

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
