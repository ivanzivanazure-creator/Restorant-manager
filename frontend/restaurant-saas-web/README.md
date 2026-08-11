# Restaurant SaaS — Web (Angular)

Standalone-components Angular app (Material, signals, functional interceptors/guards) for the
Restaurant Manager platform. See the repo root [`README.md`](../../README.md) for the full picture.

> **Note on this environment**: this app was hand-written in a sandbox whose network policy blocks
> `npm install` (registry.npmjs.org returned 403 even though it's nominally routable), so `node_modules`
> was never installed here and `ng build`/`ng test` were never run. Before relying on this app:
> ```bash
> npm install
> npm run build   # or: npm start
> npm test
> ```
> and fix anything the compiler/build flags — treat this as reviewed-but-unverified code, same caveat
> as the backend.

## Structure

```
src/app/
  core/            # auth (JWT + refresh), interceptors, guards, cross-cutting services (theme, SignalR, location context)
  shared/          # reusable UI (KPI cards, empty states)
  features/
    shell/         # sidenav + toolbar app shell
    auth/          # login, register, forgot/reset password, MFA challenge
    dashboard/     # KPIs, kitchen status, inventory alerts
    super-admin/   # tenant list, platform analytics
    restaurant-management/  # locations, tables, QR codes
    menu/          # categories, products, availability toggle
    pos/           # table map + order screen (items, discounts, tip, send-to-kitchen, payment)
    kitchen-display/  # real-time ticket queue (SignalR)
    inventory/     # stock levels by warehouse, low-stock indicators
```

## Auth & multi-tenancy on the client

- `AuthService` decodes the JWT's claims (`tenant_id`, `location_id`, `super_admin`, `permission`) into
  a `CurrentUser` signal — no separate `/me` call needed.
- `authInterceptor` attaches the bearer token; `refreshTokenInterceptor` retries once on 401 via a
  shared in-flight refresh (`core/interceptors/refresh-state.ts`) so a burst of 401s doesn't trigger a
  burst of refresh calls.
- `LocationContextService` tracks which Location the signed-in staff member is currently operating at;
  POS/KDS/Inventory/Dashboard are all scoped to it.
- Nav items and route guards check `Permissions.*` (mirrored 1:1 from the backend's
  `Application/Common/Security/Permissions.cs` — keep the two in sync).

## Theming

Light/dark mode is CSS-variable-based (`src/styles.scss`, `:root` vs `:root.dark-theme`), toggled by
`ThemeService` and defaulting to `prefers-color-scheme`. Angular Material's prebuilt `azure-blue` theme
supplies component-level colors; the CSS variables layer app-specific surfaces on top.
