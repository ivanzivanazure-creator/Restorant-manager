namespace RestaurantSaaS.Application.Common.Security;

/// <summary>
/// Flat permission-key catalogue. Each key becomes both a JWT "permission" claim value and an
/// ASP.NET Core authorization policy name (registered dynamically — see AddInfrastructure /
/// AuthorizationPolicyProvider — so a new permission here never needs a new [Authorize(Policy=...)] class).
/// </summary>
public static class Permissions
{
    public const string SuperAdminAccess = "superadmin.access";

    public static class Tenancy
    {
        public const string ManageRestaurants = "tenancy.restaurants.manage";
        public const string ManageLocations = "tenancy.locations.manage";
        public const string ManageEmployees = "tenancy.employees.manage";
    }

    public static class Subscription
    {
        public const string ManagePackages = "subscription.packages.manage";
        public const string ManageTenantSubscriptions = "subscription.tenants.manage";
        public const string ViewAnalytics = "subscription.analytics.view";
    }

    public static class Menu
    {
        public const string Manage = "menu.manage";
        public const string View = "menu.view";
    }

    public static class Pos
    {
        public const string OpenOrder = "pos.orders.open";
        public const string ModifyOrder = "pos.orders.modify";
        public const string ApplyDiscount = "pos.discounts.apply";
        public const string TakePayment = "pos.payments.take";
        public const string IssueRefund = "pos.refunds.issue";
        public const string ViewOrders = "pos.orders.view";
    }

    public static class Kitchen
    {
        public const string ManageTickets = "kitchen.tickets.manage";
        public const string ViewQueue = "kitchen.queue.view";
    }

    public static class Inventory
    {
        public const string Manage = "inventory.manage";
        public const string View = "inventory.view";
        public const string ApproveProcurement = "inventory.procurement.approve";
    }

    public static class Dashboard
    {
        public const string View = "dashboard.view";
    }

    /// <summary>All permission keys, used to seed the Permission table and to enumerate dynamic
    /// authorization policies at startup.</summary>
    public static IReadOnlyCollection<string> All { get; } =
    [
        SuperAdminAccess,
        Tenancy.ManageRestaurants, Tenancy.ManageLocations, Tenancy.ManageEmployees,
        Subscription.ManagePackages, Subscription.ManageTenantSubscriptions, Subscription.ViewAnalytics,
        Menu.Manage, Menu.View,
        Pos.OpenOrder, Pos.ModifyOrder, Pos.ApplyDiscount, Pos.TakePayment, Pos.IssueRefund, Pos.ViewOrders,
        Kitchen.ManageTickets, Kitchen.ViewQueue,
        Inventory.Manage, Inventory.View, Inventory.ApproveProcurement,
        Dashboard.View,
    ];
}
