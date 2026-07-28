using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.RestaurantOps;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;
using RestaurantSaaS.Domain.ValueObjects;
using RestaurantSaaS.Infrastructure.Identity;

namespace RestaurantSaaS.Infrastructure.Persistence.Seed;

/// <summary>Idempotent startup seeder: system permissions/roles/packages always, plus (outside
/// Production) a demo tenant with sample users so the product can be explored immediately.
/// See README.md "Seed data / sample logins" for the credential table.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        ILogger logger, bool seedDemoTenant, CancellationToken ct = default)
    {
        // Schema creation (EnsureCreated or Migrate) is the caller's responsibility — see Program.cs.
        var permissions = await SeedPermissionsAsync(db, ct);
        var systemRoles = await SeedSystemRolesAsync(db, permissions, ct);
        await SeedPackagesAsync(db, ct);

        var superAdmin = await SeedSuperAdminAsync(db, userManager, systemRoles, ct);
        logger.LogInformation("Seeded SuperAdmin {Email}", superAdmin.Email);

        if (seedDemoTenant)
        {
            await SeedDemoTenantAsync(db, userManager, systemRoles, ct);
            logger.LogInformation("Seeded demo tenant 'Bella Pizza'");
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Key, ct);
        foreach (var key in Permissions.All)
        {
            if (existing.ContainsKey(key)) continue;
            var module = key.Split('.')[0];
            var permission = new Permission(key, module, key);
            db.Permissions.Add(permission);
            existing[key] = permission;
        }
        await db.SaveChangesAsync(ct);
        return existing;
    }

    private static async Task<Dictionary<string, Role>> SeedSystemRolesAsync(
        ApplicationDbContext db, Dictionary<string, Permission> permissions, CancellationToken ct)
    {
        var definitions = new Dictionary<string, string[]>
        {
            ["SuperAdmin"] = [Permissions.SuperAdminAccess],
            ["Owner"] =
            [
                Permissions.Tenancy.ManageRestaurants, Permissions.Tenancy.ManageLocations, Permissions.Tenancy.ManageEmployees,
                Permissions.Menu.Manage, Permissions.Menu.View,
                Permissions.Pos.OpenOrder, Permissions.Pos.ModifyOrder, Permissions.Pos.ApplyDiscount, Permissions.Pos.TakePayment, Permissions.Pos.IssueRefund, Permissions.Pos.ViewOrders,
                Permissions.Kitchen.ManageTickets, Permissions.Kitchen.ViewQueue,
                Permissions.Inventory.Manage, Permissions.Inventory.View, Permissions.Inventory.ApproveProcurement,
                Permissions.Dashboard.View,
                Permissions.Billing.Manage, Permissions.Billing.View, Permissions.Integrations.Manage,
            ],
            ["Manager"] =
            [
                Permissions.Tenancy.ManageLocations, Permissions.Tenancy.ManageEmployees,
                Permissions.Menu.Manage, Permissions.Menu.View,
                Permissions.Pos.OpenOrder, Permissions.Pos.ModifyOrder, Permissions.Pos.ApplyDiscount, Permissions.Pos.TakePayment, Permissions.Pos.IssueRefund, Permissions.Pos.ViewOrders,
                Permissions.Kitchen.ManageTickets, Permissions.Kitchen.ViewQueue,
                Permissions.Inventory.Manage, Permissions.Inventory.View,
                Permissions.Dashboard.View,
                Permissions.Billing.View, Permissions.Integrations.Manage,
            ],
            ["Waiter"] = [Permissions.Pos.OpenOrder, Permissions.Pos.ModifyOrder, Permissions.Pos.TakePayment, Permissions.Pos.ViewOrders, Permissions.Menu.View],
            ["Chef"] = [Permissions.Kitchen.ManageTickets, Permissions.Kitchen.ViewQueue, Permissions.Inventory.View, Permissions.Menu.View],
            ["Cashier"] = [Permissions.Pos.TakePayment, Permissions.Pos.ViewOrders, Permissions.Pos.IssueRefund],
            ["InventoryClerk"] = [Permissions.Inventory.Manage, Permissions.Inventory.View],
        };

        var existing = await db.Roles.Where(r => r.TenantId == null).Include(r => r.RolePermissions).ToDictionaryAsync(r => r.Name, ct);

        foreach (var (name, keys) in definitions)
        {
            if (!existing.TryGetValue(name, out var role))
            {
                role = new Role(name, isSystemRole: true);
                db.Roles.Add(role);
                existing[name] = role;
            }

            foreach (var key in keys)
            {
                role.Grant(permissions[key]);
            }
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private static async Task SeedPackagesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (await db.Packages.AnyAsync(ct)) return;

        db.Packages.AddRange(
            new Package("Starter", maxUsers: 5, maxLocations: 1, monthlyPrice: 49m, yearlyPrice: 490m,
                transactionFeePercent: 1.9m, slaTier: SlaTier.Standard, slaUptimeTargetPercent: 99.5m),
            new Package("Professional", maxUsers: 10, maxLocations: 3, monthlyPrice: 99m, yearlyPrice: 990m,
                transactionFeePercent: 1.5m, slaTier: SlaTier.Standard, slaUptimeTargetPercent: 99.5m),
            new Package("Unlimited", maxUsers: null, maxLocations: int.MaxValue, monthlyPrice: 249m, yearlyPrice: 2490m,
                transactionFeePercent: 1.0m, slaTier: SlaTier.Premium, slaUptimeTargetPercent: 99.9m,
                featureFlags: new Dictionary<string, bool> { ["hotelModule"] = true, ["aiAssistant"] = true }));

        await db.SaveChangesAsync(ct);
    }

    private static async Task<ApplicationUser> SeedSuperAdminAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, Dictionary<string, Role> roles, CancellationToken ct)
    {
        const string email = "superadmin@restaurantsaas.io";
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Platform",
                LastName = "Admin",
                IsSuperAdmin = true,
            };
            await userManager.CreateAsync(user, "SuperAdmin!2026");
        }

        if (!db.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == roles["SuperAdmin"].Id))
        {
            db.UserRoles.Add(new UserRole(user.Id, roles["SuperAdmin"].Id, tenantId: null, locationId: null));
        }

        return user;
    }

    private static async Task SeedDemoTenantAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, Dictionary<string, Role> roles, CancellationToken ct)
    {
        if (await db.RestaurantOwners.AnyAsync(o => o.ContactEmail == "owner@bellapizza.demo", ct)) return;

        var ownerUser = await CreateUserAsync(userManager, "owner@bellapizza.demo", "Owner!2026", "Bella", "Owner");

        var tenant = new RestaurantOwner("Bella Pizza Group", "owner@bellapizza.demo", ownerUser.Id);
        db.RestaurantOwners.Add(tenant);

        ownerUser.TenantId = tenant.Id;
        await userManager.UpdateAsync(ownerUser);

        var subscription = new TenantSubscription(tenant.Id, (await db.Packages.SingleAsync(p => p.Name == "Professional", ct)).Id, BillingCycle.Monthly);
        subscription.Activate("cus_demo", "sub_demo", DateTimeOffset.UtcNow.AddMonths(1));
        db.TenantSubscriptions.Add(subscription);

        var restaurant = tenant.AddRestaurant("Bella Pizza", "Bella Pizza LLC", "USD");

        var downtown = restaurant.AddLocation("Downtown", "123 Main St", "Springfield", "USA", "USD");
        var uptown = restaurant.AddLocation("Uptown", "456 Elm St", "Springfield", "USA", "USD");
        downtown.SetTaxConfig(new TaxConfig(tenant.Id, downtown.Id, 8.25m, "Sales Tax"));

        var kitchenDept = new Department(tenant.Id, downtown.Id, "Kitchen");
        var floorDept = new Department(tenant.Id, downtown.Id, "Front of House");
        db.Departments.AddRange(kitchenDept, floorDept);

        // Tables with a simple grid layout
        for (var i = 1; i <= 8; i++)
        {
            var table = new Table(tenant.Id, downtown.Id, $"T{i}", capacity: i % 2 == 0 ? 4 : 2, TableShape.Round, x: (i % 4) * 120, y: (i / 4) * 120);
            db.Tables.Add(table);
        }

        // Menu
        var pizzaCategory = new MenuCategory(tenant.Id, downtown.Id, "Pizza", 1);
        var margherita = pizzaCategory.AddProduct("Margherita", "Tomato, mozzarella, basil", Money.Of(12.50m, "USD"));
        margherita.AddVariant("Large", 4.00m);
        var toppings = margherita.AddModifierGroup("Extra Toppings", isRequired: false, maxSelections: 5);
        toppings.AddModifier("Extra Cheese", 1.50m);
        toppings.AddModifier("Mushrooms", 1.00m);
        var pepperoni = pizzaCategory.AddProduct("Pepperoni", "Tomato, mozzarella, pepperoni", Money.Of(14.00m, "USD"));
        pepperoni.AddVariant("Large", 4.00m);

        var drinksCategory = new MenuCategory(tenant.Id, downtown.Id, "Drinks", 2);
        drinksCategory.AddProduct("Soda", "Assorted", Money.Of(3.00m, "USD"));

        db.MenuCategories.AddRange(pizzaCategory, drinksCategory);

        // Inventory
        var warehouse = new Warehouse(tenant.Id, downtown.Id, "Main Storage");
        db.Warehouses.Add(warehouse);

        var flour = new Ingredient(tenant.Id, "Flour", MeasurementUnit.Kilogram, reorderThreshold: 10m, costPerUnit: 0.80m);
        var mozzarella = new Ingredient(tenant.Id, "Mozzarella", MeasurementUnit.Kilogram, reorderThreshold: 5m, costPerUnit: 6.50m);
        db.Ingredients.AddRange(flour, mozzarella);

        var flourStock = new StockLevel(tenant.Id, warehouse.Id, flour.Id);
        flourStock.Receive(50m, expiresAt: DateTimeOffset.UtcNow.AddMonths(6), unitCost: 0.80m);
        var mozzStock = new StockLevel(tenant.Id, warehouse.Id, mozzarella.Id);
        mozzStock.Receive(15m, expiresAt: DateTimeOffset.UtcNow.AddDays(14), unitCost: 6.50m);
        db.StockLevels.AddRange(flourStock, mozzStock);

        // Staff
        var managerUser = await CreateUserAsync(userManager, "manager@bellapizza.demo", "Manager!2026", "Maria", "Manager");
        var waiterUser = await CreateUserAsync(userManager, "waiter@bellapizza.demo", "Waiter!2026", "Will", "Waiter");
        var chefUser = await CreateUserAsync(userManager, "chef@bellapizza.demo", "Chef!2026", "Carlos", "Chef");

        foreach (var (user, roleName) in new[] { (managerUser, "Manager"), (waiterUser, "Waiter"), (chefUser, "Chef") })
        {
            user.TenantId = tenant.Id;
            user.DefaultLocationId = downtown.Id;
            await userManager.UpdateAsync(user);
            db.UserRoles.Add(new UserRole(user.Id, roles[roleName].Id, tenant.Id, downtown.Id));
        }

        db.Employees.AddRange(
            new Domain.Tenancy.Employee(tenant.Id, floorDept.Id, downtown.Id, managerUser.Id, "Maria", "Manager", "General Manager", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), null),
            new Domain.Tenancy.Employee(tenant.Id, floorDept.Id, downtown.Id, waiterUser.Id, "Will", "Waiter", "Waiter", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-8)), 16.50m),
            new Domain.Tenancy.Employee(tenant.Id, kitchenDept.Id, downtown.Id, chefUser.Id, "Carlos", "Chef", "Head Chef", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)), 24.00m));

        _ = uptown; // reserved for future multi-location demo data (Phase 2 seed expansion)
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string firstName, string lastName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }
}
