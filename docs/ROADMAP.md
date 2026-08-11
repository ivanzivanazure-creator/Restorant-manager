# Roadmap — what's built vs. planned

This delivery ("Phase 1") is scoped for genuine end-to-end depth on the modules a restaurant
needs to run day one, rather than shallow stubs across all 16 modules. The full domain model
already covers every module (see [`ERD.md`](ERD.md)), so Phase 2/3 is additive — no rework of the
schema should be needed, only new Application/API/UI layers on top of existing entities.

## Phase 1 — this delivery

Fully implemented (Domain + Application/CQRS + API + real-time where relevant + Angular UI + tests):

- [x] Multi-tenancy core (Super Admin → Restaurant Owner → Restaurant → Location → Department → Employee)
- [x] Auth: register, login, refresh tokens, forgot/reset password, TOTP MFA, permission-based RBAC
- [x] Subscription: packages, free trial, Stripe-ready billing surface, expiration auto-lock job, invoices
- [x] Restaurant Management: profile, locations, working hours, tax/currency, tables, table layout, QR codes
- [x] Menu: categories, products, variants, modifiers, allergens, availability windows
- [x] POS: open table, orders, split bill, merge tables, discounts, tips, cash/card/voucher payments, refunds, automatic inventory deduction on order completion
- [x] Kitchen Display: real-time queue (SignalR), priority, cooking timer, ready notification
- [x] Inventory (core): warehouses, ingredients, stock levels, stock movements, FIFO batches, purchase orders + goods receipt
- [x] Dashboard: revenue, today's sales, KPIs, kitchen status, inventory alerts, onboarding checklist
- [x] Super Admin portal: tenant/package management, activate/deactivate, analytics, incident management
- [x] Billing: Stripe Connect onboarding, usage-based platform fee (take-rate) on card payments,
      per-tenant billing summary — see "Billing & platform fees" in `docs/ARCHITECTURE.md`
- [x] Delivery platform integrations: webhook-based order ingestion (generic payload, name-matched
      against the tenant's menu) feeding the same Order/POS/KDS pipeline as an in-house order
- [x] Public status page + incident history, SLA tier per package

## Phase 2 — modeled in Domain now, needs Application/API/UI

- [ ] **Recipes** — full builder UI (steps, photos/video, cook timers), recipe versioning, automatic cost
      recalculation when ingredient prices change
- [ ] **Procurement** — supplier contracts/price lists, approval workflow, automatic reorder suggestions
      driven off `Ingredient.ReorderThreshold` + sales velocity
- [ ] **Hotel module** — reservations, check-in/out, room service ordering, minibar charges, room-linked
      table billing, guest stay history (entities exist: `Room`, `Reservation`, `Guest`, `GuestStay`,
      `RoomServiceOrder`, `MinibarCharge`)
- [ ] **CRM** — loyalty points engine, coupons, gift cards, feedback/review collection & moderation
- [ ] **Employees (advanced)** — shift planning UI, attendance/time-clock, payroll runs, bonuses,
      vacation/sick leave approval, performance reviews
- [ ] **Reports (full)** — profit/waste/tax reports, PDF/Excel export (entities + raw SQL projections
      exist for sales/inventory; export pipeline and the remaining report types are not built)
- [ ] **Notifications (delivery)** — SMS (Twilio) and Push (FCM/APNs) provider adapters; email adapter
      is implemented (SMTP + templated), the interface (`INotificationSender`) is provider-agnostic so
      SMS/Push are additive
- [ ] **Per-platform delivery adapters** — UberEats and DoorDash each have their own webhook payload
      shape and their own SKU/menu-sync APIs; today's `IngestDeliveryOrderCommand` takes a generic,
      already-normalized payload and matches items by product name. A real go-live needs one small
      adapter per platform that translates their actual webhook JSON into that generic shape (and
      ideally a SKU mapping table instead of name matching)
- [ ] **Tokenized in-app payment capture** — `IPaymentGatewayService.CapturePaymentWithApplicationFeeAsync`
      is implemented and ready, but nothing calls it yet: today's POS assumes card payments are captured
      by the restaurant's own card terminal outside this system, and `PayOrderCommandHandler` only
      records the platform-fee ledger entry for reporting/reconciliation. Wiring a real charge (QR
      self-order checkout with Stripe Elements, or Stripe Terminal for card-present POS) is additive

## Phase 3

- [ ] **AI Assistant** — inventory need prediction, sales forecasting, purchasing suggestions, anomaly
      detection, price optimization, profitable-dish recommendations, natural-language report
      generation. Planned approach: Azure OpenAI for NL report generation + ML.NET/forecasting models
      for the numeric predictions, fed from the `DailySalesSummary` / stock-movement history already
      captured by Phase 1's background jobs.
- [ ] Multi-region deployment guidance, read replicas for reporting workloads
- [ ] Public partner API / webhooks for third-party integrations (delivery platforms, accounting)

## Why this split

Building all 16 modules to equal, sellable depth in a single pass isn't achievable without producing
stub-quality code everywhere, which the project brief explicitly rules out ("never simplify"). This
split gives you a real, runnable core product (the modules a single-location restaurant needs to
open for business) plus a schema that already anticipates every module in the brief, so Phase 2/3
is pure addition, not migration.
