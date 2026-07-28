using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Domain.Billing;
using RestaurantSaaS.Domain.Crm;
using RestaurantSaaS.Domain.Employees;
using RestaurantSaaS.Domain.Hotel;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Domain.Integrations;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Notifications;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Procurement;
using RestaurantSaaS.Domain.Recipes;
using RestaurantSaaS.Domain.Reporting;
using RestaurantSaaS.Domain.RestaurantOps;
using RestaurantSaaS.Domain.Status;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;

namespace RestaurantSaaS.Application.Common.Interfaces;

/// <summary>
/// Port implemented by RestaurantSaaS.Infrastructure's ApplicationDbContext. Application handlers depend
/// on this abstraction (not EF Core directly) so they stay testable with an in-memory/fake implementation.
/// Explicit DbSets are declared for the aggregates handlers query directly; anything else is reachable via
/// the generic Set&lt;TEntity&gt;() escape hatch, mirroring EF Core's own DbContext API.
/// </summary>
public interface IApplicationDbContext
{
    // Tenancy
    DbSet<RestaurantOwner> RestaurantOwners { get; }
    DbSet<Restaurant> Restaurants { get; }
    DbSet<Location> Locations { get; }
    DbSet<Department> Departments { get; }
    DbSet<Employee> Employees { get; }

    // Identity / RBAC
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<MfaEnrollment> MfaEnrollments { get; }

    // Subscription
    DbSet<Package> Packages { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<Invoice> Invoices { get; }

    // Restaurant ops
    DbSet<Table> Tables { get; }

    // Menu
    DbSet<MenuCategory> MenuCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }

    // POS
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }

    // Kitchen
    DbSet<KitchenTicket> KitchenTickets { get; }

    // Inventory
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Ingredient> Ingredients { get; }
    DbSet<StockLevel> StockLevels { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<Supplier> Suppliers { get; }

    // Recipes
    DbSet<Recipe> Recipes { get; }

    // Hotel
    DbSet<Room> Rooms { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<GuestStay> GuestStays { get; }

    // CRM
    DbSet<Customer> Customers { get; }
    DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<GiftCard> GiftCards { get; }

    // Employees ops
    DbSet<Shift> Shifts { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }

    // Notifications / Reporting
    DbSet<Notification> Notifications { get; }
    DbSet<DailySalesSummary> DailySalesSummaries { get; }

    // Billing / Integrations / Status
    DbSet<PlatformFeeLedgerEntry> PlatformFeeLedgerEntries { get; }
    DbSet<DeliveryIntegration> DeliveryIntegrations { get; }
    DbSet<SystemIncident> SystemIncidents { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
