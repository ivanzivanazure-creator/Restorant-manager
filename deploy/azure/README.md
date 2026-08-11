# Azure deployment

`main.bicep` provisions the platform's Azure footprint. This is a **starting point** — review SKUs,
scaling, and networking (especially private endpoints for Postgres/Redis in production) before using
it for a real deployment.

## Resources provisioned

| Resource | Purpose |
|---|---|
| Container Apps Environment + 3 Container Apps (`api`, `worker`, `web`) | Compute — see `docs/ARCHITECTURE.md` for why Container Apps over App Service at thousands-of-tenants scale |
| Azure Database for PostgreSQL — Flexible Server | Primary datastore, shared schema / row-level tenant isolation |
| Azure Cache for Redis | Distributed cache, SignalR backplane, rate-limiter store |
| Azure SignalR Service | Offloads real-time hub connections (Kitchen Display, Orders) from the API's own compute |
| Azure Key Vault | JWT signing key, DB connection string, Stripe/SMTP secrets — referenced via each Container App's system-assigned managed identity, never as plain App Settings |
| Azure Container Registry (referenced, not provisioned here — see below) | Container image storage |
| Log Analytics + Application Insights | Centralized logs/traces/metrics |
| Storage Account | Product images, recipe photos, invoice PDFs (swap `LocalFileStorageService` for an Azure Blob implementation behind the same `IFileStorageService` interface) |

ACR itself is intentionally *not* provisioned by this template (create it once, separately, since it
outlives any single environment's lifecycle):

```bash
az acr create --resource-group <rg> --name <uniqueAcrName> --sku Standard
```

## Deploying

1. Create the resource group: `az group create --name <rg> --location <region>`
2. Build & push the three images to ACR (see `.github/workflows/deploy-azure.yml` for the exact steps,
   or do it manually with `az acr build`).
3. Deploy:
   ```bash
   az deployment group create \
     --resource-group <rg> \
     --template-file deploy/azure/main.bicep \
     --parameters acrLoginServer=<acr>.azurecr.io \
                  postgresAdminPassword=<generate-a-strong-one> \
                  jwtSigningKey=<generate-64-random-bytes-base64> \
                  environment=staging
   ```
4. Run EF Core migrations against the new Postgres instance (see the `TODO` in
   `backend/src/Presentation/RestaurantSaaS.Api/Program.cs` — generate real migrations before this
   step; `EnsureCreatedAsync` is not appropriate for a production database you intend to evolve).
5. Grant each Container App's managed identity `Key Vault Secrets User` on the Key Vault, and
   `AcrPull` on the registry (the Bicep template references `identity: 'system'` for registry auth;
   you still need to assign the RBAC roles — Bicep doesn't do this for you across resource providers
   in one pass without a role assignment resource, omitted here for brevity).

## Not yet in this template

- Custom domains + managed TLS certificates on the Container Apps
- Private networking (VNet integration, private endpoints for Postgres/Redis) — recommended before
  handling real customer data in production
- Autoscale rules beyond the default HTTP-concurrency scaler
- A staging slot / blue-green deploy strategy — today a new image tag simply creates a new revision
