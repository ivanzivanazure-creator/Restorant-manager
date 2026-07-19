using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Audit;
using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Crm;
using RestaurantSaaS.Domain.Employees;
using RestaurantSaaS.Domain.Hotel;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Notifications;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Procurement;
using RestaurantSaaS.Domain.Recipes;
using RestaurantSaaS.Domain.Reporting;
using RestaurantSaaS.Domain.RestaurantOps;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;
using RestaurantSaaS.Infrastructure.Identity;

namespace RestaurantSaaS.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    // Tenancy
    public DbSet<RestaurantOwner> RestaurantOwners => Set<RestaurantOwner>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Identity / RBAC
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MfaEnrollment> MfaEnrollments => Set<MfaEnrollment>();

    // Subscription
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Restaurant ops
    public DbSet<Table> Tables => Set<Table>();

    // Menu
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    // POS
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Kitchen
    public DbSet<KitchenTicket> KitchenTickets => Set<KitchenTicket>();

    // Inventory
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Recipes
    public DbSet<Recipe> Recipes => Set<Recipe>();

    // Hotel
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<GuestStay> GuestStays => Set<GuestStay>();

    // CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    // Employees ops
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    // Notifications / Reporting / Audit
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DailySalesSummary> DailySalesSummaries => Set<DailySalesSummary>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Rename ASP.NET Identity's default tables to a consistent snake_case schema.
        builder.Entity<ApplicationUser>(b => b.ToTable("users"));
        builder.Entity<IdentityRole<Guid>>(b => b.ToTable("identity_roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("identity_user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("identity_user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("identity_user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("identity_role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("identity_user_tokens"));

        // Row-level multi-tenancy: every IMustHaveTenant entity gets a global query filter that scopes
        // reads to the caller's tenant, unless the caller is SuperAdmin. See docs/ARCHITECTURE.md.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType)) continue;
            typeof(ApplicationDbContext)
                .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [builder]);
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, IMustHaveTenant
    {
        builder.Entity<TEntity>().HasQueryFilter(e => tenantProvider.IsSuperAdmin || e.TenantId == tenantProvider.TenantId);
    }

    Task<int> IApplicationDbContext.SaveChangesAsync(CancellationToken cancellationToken) => base.SaveChangesAsync(cancellationToken);
}
