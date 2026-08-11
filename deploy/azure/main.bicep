// Restaurant SaaS — Azure deployment skeleton.
// Provisions: Container Apps Environment (api/worker/web), Azure Database for PostgreSQL Flexible
// Server, Azure Cache for Redis, Azure SignalR Service, Azure Container Registry, Key Vault, Log
// Analytics + Application Insights, and a Storage Account (for product/recipe images, invoice PDFs).
//
// This is a starting point, not a drop-in-and-deploy template: fill in `main.parameters.json`,
// review SKUs/sizing for your expected tenant count, and wire the GitHub Actions secrets referenced
// in .github/workflows/deploy-azure.yml. See deploy/azure/README.md.

@description('Short environment name, e.g. staging, production')
param environment string = 'staging'

@description('Azure region')
param location string = resourceGroup().location

@description('Container image tag to deploy (set by CI to the git SHA)')
param imageTag string = 'latest'

@description('ACR login server, e.g. myregistry.azurecr.io')
param acrLoginServer string

@secure()
@description('PostgreSQL administrator password')
param postgresAdminPassword string

@secure()
@description('JWT signing key (32+ random bytes, base64)')
param jwtSigningKey string

var namePrefix = 'rsaas-${environment}'
var tags = {
  application: 'restaurant-saas'
  environment: environment
}

// ---- Observability ----

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-ai'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---- Data services ----

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: '${namePrefix}-pg'
  location: location
  tags: tags
  sku: {
    name: 'Standard_D2ds_v4' // right-size per tenant count / workload; burstable B1ms is fine for staging
    tier: 'GeneralPurpose'
  }
  properties: {
    version: '16'
    administratorLogin: 'restaurant_saas_admin'
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: 128 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: environment == 'production' ? 'ZoneRedundant' : 'Disabled' }
  }
}

resource postgresDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgres
  name: 'restaurant_saas'
}

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: '${namePrefix}-redis'
  location: location
  tags: tags
  properties: {
    sku: { name: 'Standard', family: 'C', capacity: 1 }
    minimumTlsVersion: '1.2'
  }
}

resource signalR 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: '${namePrefix}-signalr'
  location: location
  tags: tags
  sku: { name: 'Standard_S1', tier: 'Standard', capacity: 1 }
  kind: 'SignalR'
  properties: {
    features: [{ flag: 'ServiceMode', value: 'Default' }]
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${namePrefix}st', '-', '')
  location: location
  tags: tags
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// ---- Secrets ----

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${namePrefix}-kv'
  location: location
  tags: tags
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-signing-key'
  properties: { value: jwtSigningKey }
}

resource postgresSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-connection-string'
  properties: {
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=restaurant_saas;Username=restaurant_saas_admin;Password=${postgresAdminPassword};SSL Mode=Require'
  }
}

// ---- Compute (Container Apps) ----

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-env'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${namePrefix}-api'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'auto' }
      registries: [{ server: acrLoginServer, identity: 'system' }]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acrLoginServer}/restaurant-saas-api:${imageTag}'
          resources: { cpu: json('1.0'), memory: '2Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: environment == 'production' ? 'Production' : 'Staging' }
            { name: 'ConnectionStrings__Postgres', secretRef: 'postgres-connection-string' }
            { name: 'ConnectionStrings__Redis', value: '${redis.properties.hostName}:6380,ssl=true,password=${redis.listKeys().primaryKey}' }
            { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
            { name: 'ApplicationInsights__ConnectionString', value: appInsights.properties.ConnectionString }
          ]
        }
      ]
      scale: { minReplicas: environment == 'production' ? 2 : 1, maxReplicas: 10 }
    }
  }
  identity: { type: 'SystemAssigned' }
}

resource workerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${namePrefix}-worker'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: null
      registries: [{ server: acrLoginServer, identity: 'system' }]
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: '${acrLoginServer}/restaurant-saas-workers:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ConnectionStrings__Postgres', secretRef: 'postgres-connection-string' }
            { name: 'ConnectionStrings__Redis', value: '${redis.properties.hostName}:6380,ssl=true,password=${redis.listKeys().primaryKey}' }
            { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 } // single Hangfire server instance; scale via queues, not replicas
    }
  }
  identity: { type: 'SystemAssigned' }
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${namePrefix}-web'
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: { external: true, targetPort: 80, transport: 'auto' }
      registries: [{ server: acrLoginServer, identity: 'system' }]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${acrLoginServer}/restaurant-saas-web:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
  identity: { type: 'SystemAssigned' }
}

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output webUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
