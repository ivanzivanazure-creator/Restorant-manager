# Architecture

## Layering (Clean Architecture)

```
 ┌─────────────────────────────────────────────────────────────┐
 │  Presentation:  RestaurantSaaS.Api (controllers, SignalR)    │
 │  Workers:       RestaurantSaaS.Workers (Hangfire jobs)       │
 └───────────────────────────┬─────────────────────────────────┘
                              │ depends on
 ┌───────────────────────────▼─────────────────────────────────┐
 │  Infrastructure: RestaurantSaaS.Infrastructure               │
 │  EF Core / PostgreSQL, Identity, JWT, Redis, SignalR,        │
 │  Hangfire storage, Serilog sinks, Stripe/SMS/Email adapters  │
 └───────────────────────────┬─────────────────────────────────┘
                              │ implements interfaces from
 ┌───────────────────────────▼─────────────────────────────────┐
 │  Application: RestaurantSaaS.Application                     │
 │  CQRS commands/queries + MediatR handlers, FluentValidation, │
 │  DTOs, Mapster profiles, port interfaces (IAppDbContext,     │
 │  ITenantProvider, ICurrentUser, IDateTime, INotificationSvc) │
 └───────────────────────────┬─────────────────────────────────┘
                              │ depends on
 ┌───────────────────────────▼─────────────────────────────────┐
 │  Domain: RestaurantSaaS.Domain                                │
 │  Entities, value objects, enums, domain events — zero deps   │
 └────────────────────────────────────────────────────────────┘
```

Dependency rule: arrows point inward only. `Domain` references nothing else in the solution.
`Application` references `Domain` and defines interfaces that `Infrastructure` implements
(Dependency Inversion). `Api` and `Workers` wire everything together at the composition root
(`Program.cs`) via `AddApplication()`, `AddInfrastructure()` extension methods.

## Multi-tenancy

- **Isolation**: shared database, shared schema, row-level security via `TenantId` column +
  EF Core global query filters (`IMustHaveTenant` marker interface, filter applied in
  `ApplicationDbContext.OnModelCreating`).
- **Tenant resolution**: `ITenantProvider` reads `tenant_id` claim from the authenticated JWT.
  Super Admin JWTs carry a `super_admin` claim instead and bypass the filter entirely
  (`IgnoreQueryFilters()` gated behind an authorization check, never exposed directly to clients).
- **Enforcement redundancy**: in addition to the query filter, a `SaveChangesInterceptor`
  (`TenantSaveChangesInterceptor`) stamps `TenantId` on every new `IMustHaveTenant` entity and
  rejects (throws `CrossTenantAccessException`) any attempt to attach/modify an entity whose
  `TenantId` doesn't match the current tenant — defense in depth against a missing filter on a
  raw SQL/bulk path.
- **Hierarchy**: `RestaurantOwner` (the billable tenant) → `Restaurant` (brand) → `Location`
  (branch, its own address/working hours/tax config) → `Department` → `Employee`. A `RestaurantOwner`
  can own multiple `Restaurant`s (e.g. a group running a pizzeria brand and a bar brand under one
  subscription).

## Authentication & authorization

- ASP.NET Core Identity (`IdentityUser<Guid>` derivative `ApplicationUser`) backs local accounts.
- **Access tokens**: short-lived JWT (15 min default), signed HMAC-SHA256 (key from config/Key Vault),
  claims include `sub`, `tenant_id`, `restaurant_id`, `location_id`, `role`, and flattened
  `permission` claims for fast authorization without a DB round-trip per request.
- **Refresh tokens**: opaque, random 256-bit tokens, hashed at rest, stored per-device with
  expiry/rotation (`RefreshToken` entity) — rotated on every use, previous token revoked
  (detects token replay/theft).
- **MFA**: TOTP (RFC 6238) via `Otp.NET`; `MfaEnrollment` entity stores the encrypted secret and
  recovery codes; login flow issues a short-lived `mfa_pending` token until the TOTP challenge passes.
- **RBAC**: permission-based, not just role-name based. `Role` → many `Permission`s (seeded:
  Owner, Manager, Waiter, Chef, Cashier, InventoryClerk, HR, SuperAdmin, ...). Controllers use
  `[Authorize(Policy = Permissions.Pos.CreateOrder)]`-style policies registered dynamically from
  the `Permission` enum so a new permission never needs a new policy class.

## CQRS & validation pipeline

MediatR pipeline behaviors, in order: `LoggingBehavior` → `ValidationBehavior`
(FluentValidation, throws `ValidationException` → mapped to 400) → `TenantAuthorizationBehavior`
(rejects commands/queries targeting a different tenant than the caller's) →
`UnhandledExceptionBehavior` (logs + rethrows for the global exception middleware) → handler.
Commands mutate via the `IAppDbContext` (unit of work = `SaveChangesAsync` at the end of the
request, one transaction per request). Queries go straight to EF Core / Dapper read models where
projection performance matters (e.g. dashboard aggregates).

## Real-time

SignalR hubs, backed by a Redis backplane (so it scales horizontally across API instances):

- `OrdersHub` — waiter/table-side clients: order status changes, "ready to serve" pushes.
- `KitchenHub` — Kitchen Display clients: new order pushed the instant POS confirms it, status
  transitions (`Queued → InProgress → Ready → Served`), priority changes, cooking timers.

Both hubs are driven by MediatR `INotification`s raised from command handlers (e.g.
`OrderPlacedEvent` → `KitchenHub` group for the order's `LocationId`), not called directly from
controllers, keeping the transport swappable.

## Caching

Redis (`IDistributedCache` + a typed `ICacheService` wrapper) caches: menu reads (invalidated on
menu command handlers via cache-aside + explicit key invalidation), dashboard aggregates (30s TTL),
and permission lookups. Session-like state (SignalR backplane, rate-limiter store) also lives in
Redis so the API is horizontally scalable / stateless.

## Background jobs (Hangfire)

Recurring jobs registered in `RestaurantSaaS.Workers`:
- `SubscriptionExpirationJob` (hourly) — locks tenants whose subscription lapsed past grace period.
- `LowStockAlertJob` (every 15 min) — scans `StockLevel` vs `Ingredient.ReorderThreshold`, raises
  `NotificationService` alerts + suggests a purchase order.
- `DailyReportAggregationJob` (nightly) — materializes `DailySalesSummary` rows for fast dashboard/report queries.

Hangfire storage is PostgreSQL (`Hangfire.PostgreSql`), dashboard exposed only to SuperAdmin/Owner roles.

## Observability

Serilog structured logging (console + rolling file + optional Seq/Application Insights sink),
enriched with `TenantId`, `CorrelationId` (from `X-Correlation-Id` header or generated),
`RequestPath`. `AuditLog` domain entity records who-did-what-when for sensitive mutations
(subscription changes, refunds, inventory corrections, employee/role changes).

## API design

REST + OpenAPI (Swashbuckle), versioned via URL segment (`/api/v1/...`), problem-details (RFC 7807)
error responses, health checks at `/health` (liveness) and `/health/ready` (DB + Redis readiness),
rate limiting (ASP.NET Core built-in fixed-window per-tenant).

## Frontend architecture

Angular, standalone components (no NgModules), lazy-loaded feature routes per module, Angular
Material + a thin design-token layer for light/dark theming (`prefers-color-scheme` default +
manual toggle persisted per user). `core/` holds the `AuthService`/JWT interceptor/refresh-on-401
interceptor/tenant-aware `HttpClient` wrapper and route guards (`authGuard`, `permissionGuard`).
`shared/` holds the reusable UI kit (data tables, KPI cards, empty states). State is
service-with-signals based (Angular signals) per feature rather than a global store, since each
feature's state is naturally tenant/location-scoped and doesn't need cross-feature sharing beyond
`AuthService`/`TenantContextService`.

## Deployment target (Azure)

- **API**: Azure Container Apps (scales to zero-ish per tenant load spikes, cheaper than App Service
  Premium at thousands-of-tenants scale) — see `deploy/azure/main.bicep`.
- **Database**: Azure Database for PostgreSQL — Flexible Server.
- **Cache/backplane**: Azure Cache for Redis.
- **Real-time**: Azure SignalR Service in "Default" mode (offloads the hub connections from the API instances).
- **Secrets**: Azure Key Vault, referenced via managed identity — no secrets in App Settings.
- **CI/CD**: GitHub Actions (`.github/workflows/ci.yml`) builds/tests both backend and frontend on
  every PR; a separate deploy workflow (documented, not wired to real credentials) pushes container
  images to Azure Container Registry and triggers a Container Apps revision update.
