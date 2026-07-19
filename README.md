# Restaurant Manager — Multi-Tenant Restaurant Management SaaS

A cloud-native, multi-tenant platform for restaurants, cafés, bars, hotels, resorts, fast-food chains,
pizzerias and bakeries: POS, Kitchen Display, Inventory, Recipes, Menu, Procurement, Hotel operations,
CRM, Employee management, Reporting and a real-time Dashboard, all behind a subscription-billed
Super Admin control plane.

> **Build status of this repo**: this is the Phase 1 foundation. See [`docs/ROADMAP.md`](docs/ROADMAP.md)
> for exactly what is fully implemented end-to-end vs. modeled-but-not-yet-built. See
> [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the technical design and
> [`docs/ERD.md`](docs/ERD.md) for the full data model.
>
> **Note on this environment**: the sandbox this codebase was generated in has no .NET SDK and no
> outbound access to install one, so the backend could not be compiled or unit-tested here. The
> Angular frontend *was* scaffolded and built for real via npm. Before relying on the backend, run
> `dotnet build` / `dotnet test` locally or in CI (a GitHub Actions workflow is included) and fix
> anything the compiler flags — treat this as reviewed-but-unverified code.

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API, Clean Architecture, DDD, CQRS (MediatR), EF Core, PostgreSQL, Redis, SignalR, Hangfire, Serilog, FluentValidation |
| Auth | ASP.NET Core Identity, JWT access + refresh tokens, TOTP MFA, permission-based RBAC |
| Frontend | Angular (standalone components), Angular Material, RxJS, responsive, light/dark theme |
| Billing | Stripe-ready subscription/invoicing module |
| Infra | Docker, Docker Compose, Azure (App Service / Container Apps, Azure DB for PostgreSQL, Azure Cache for Redis, Azure SignalR Service), GitHub Actions CI/CD |
| Testing | xUnit, FluentAssertions, NSubstitute, Testcontainers (integration), Angular/Jasmine (frontend) |

## Repository layout

```
backend/
  RestaurantSaaS.sln
  src/
    Core/
      RestaurantSaaS.Domain/          # Entities, value objects, enums, domain events — no dependencies
      RestaurantSaaS.Application/     # CQRS commands/queries/handlers, DTOs, validators, interfaces
    Infrastructure/
      RestaurantSaaS.Infrastructure/  # EF Core, Identity, Redis, SignalR, Hangfire, Serilog, external services
    Presentation/
      RestaurantSaaS.Api/             # ASP.NET Core Web API — controllers, hubs, middleware, Program.cs
    Workers/
      RestaurantSaaS.Workers/         # Hangfire background job host
  tests/
    RestaurantSaaS.Domain.UnitTests/
    RestaurantSaaS.Application.UnitTests/
    RestaurantSaaS.IntegrationTests/

frontend/
  restaurant-saas-web/                # Angular app (Material, standalone components, lazy-loaded features)

deploy/
  azure/                              # Bicep templates + deployment notes

docs/
  ARCHITECTURE.md
  ERD.md
  ROADMAP.md

docker-compose.yml
.github/workflows/ci.yml
```

## Multi-tenant hierarchy

```
Super Admin
   └── Restaurant Owner (Tenant)
          └── Restaurant (brand)
                 └── Location (branch)
                        └── Department
                               └── Employee
```

Isolation model: **single PostgreSQL database, row-level tenant isolation**. Every tenant-scoped
entity carries a `TenantId` (the owning `RestaurantOwnerId`); EF Core global query filters plus an
`ITenantProvider` resolved from JWT claims enforce isolation on every query automatically. Super
Admin principals carry a claim that bypasses the filter. This is the standard pattern for a platform
meant to run thousands of tenants economically — schema- or database-per-tenant does not scale
operationally at that count (migrations alone become the bottleneck).

## Getting started (local)

### Prerequisites
- .NET 9 SDK
- Node.js 20+ and npm
- Docker & Docker Compose
- PostgreSQL 16 and Redis (or just use `docker compose up postgres redis`)

### Run everything with Docker Compose

```bash
cp .env.example .env   # fill in secrets (JWT signing key, Stripe test key, etc.)
docker compose up --build
```

This starts: `postgres`, `redis`, `api` (http://localhost:5000, Swagger at `/swagger`),
`worker` (Hangfire dashboard at http://localhost:5001/hangfire), and `web` (Angular, http://localhost:4200).

On first run the API creates the schema (`EnsureCreatedAsync`, straight from the current EF Core model —
see the `TODO` at the top of `Program.cs`: no versioned migrations exist yet, since this repo was
generated in a sandbox with no .NET SDK / no network access to `dotnet-ef`; generate a real
`InitialCreate` migration and switch to `MigrateAsync` before your first production deploy) and seeds
demo data — see [Seed data / sample logins](#seed-data--sample-logins) below.

### Run backend locally without Docker

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/Presentation/RestaurantSaaS.Api
```

### Run frontend locally without Docker

```bash
cd frontend/restaurant-saas-web
npm install
npm start   # http://localhost:4200, proxies /api to the backend (see proxy.conf.json)
```

### Run tests

```bash
cd backend && dotnet test
cd frontend/restaurant-saas-web && npm test
```

## Seed data / sample logins

The seeder (`RestaurantSaaS.Infrastructure/Persistence/Seed/DbSeeder.cs`) creates:

| Role | Email | Password | Scope |
|---|---|---|---|
| Super Admin | `superadmin@restaurantsaas.io` | `SuperAdmin!2026` | Platform-wide |
| Restaurant Owner | `owner@bellapizza.demo` | `Owner!2026` | "Bella Pizza" tenant (2 locations) |
| Manager | `manager@bellapizza.demo` | `Manager!2026` | Bella Pizza — Downtown location |
| Waiter | `waiter@bellapizza.demo` | `Waiter!2026` | Bella Pizza — Downtown location |
| Chef | `chef@bellapizza.demo` | `Chef!2026` | Bella Pizza — Downtown kitchen |

Demo tenant ships with: 2 locations, a full table layout with QR codes, a pizzeria menu
(categories/products/variants/modifiers), starter inventory (ingredients + a warehouse + stock),
and an active "Professional / Monthly" subscription.

**Never reuse these credentials or seeded secrets in a real deployment.**

## Documentation

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — layering, multi-tenancy, auth, real-time design, caching, background jobs
- [`docs/ERD.md`](docs/ERD.md) — entity-relationship diagram (Mermaid) covering all 16 modules
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — what's fully built vs. domain-only in this delivery, and the Phase 2/3 plan
- Swagger / OpenAPI — `/swagger` on the running API

## License

Proprietary — © the repository owner. Not for redistribution without permission.
