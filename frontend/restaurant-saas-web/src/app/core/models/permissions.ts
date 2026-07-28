/** Mirrors backend/src/Core/RestaurantSaaS.Application/Common/Security/Permissions.cs — keep in sync. */
export const Permissions = {
  SuperAdminAccess: 'superadmin.access',
  Tenancy: {
    ManageRestaurants: 'tenancy.restaurants.manage',
    ManageLocations: 'tenancy.locations.manage',
    ManageEmployees: 'tenancy.employees.manage',
  },
  Subscription: {
    ManagePackages: 'subscription.packages.manage',
    ManageTenantSubscriptions: 'subscription.tenants.manage',
    ViewAnalytics: 'subscription.analytics.view',
  },
  Menu: {
    Manage: 'menu.manage',
    View: 'menu.view',
  },
  Pos: {
    OpenOrder: 'pos.orders.open',
    ModifyOrder: 'pos.orders.modify',
    ApplyDiscount: 'pos.discounts.apply',
    TakePayment: 'pos.payments.take',
    IssueRefund: 'pos.refunds.issue',
    ViewOrders: 'pos.orders.view',
  },
  Kitchen: {
    ManageTickets: 'kitchen.tickets.manage',
    ViewQueue: 'kitchen.queue.view',
  },
  Inventory: {
    Manage: 'inventory.manage',
    View: 'inventory.view',
    ApproveProcurement: 'inventory.procurement.approve',
  },
  Dashboard: {
    View: 'dashboard.view',
  },
  Billing: {
    Manage: 'billing.manage',
    View: 'billing.view',
  },
  Integrations: {
    Manage: 'integrations.manage',
  },
} as const;
