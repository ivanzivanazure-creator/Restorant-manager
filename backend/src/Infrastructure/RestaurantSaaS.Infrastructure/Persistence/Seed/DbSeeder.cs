using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Domain.Crm;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Reporting;
using RestaurantSaaS.Domain.RestaurantOps;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;
using RestaurantSaaS.Domain.ValueObjects;
using RestaurantSaaS.Infrastructure.Identity;

namespace RestaurantSaaS.Infrastructure.Persistence.Seed;

/// <summary>Idempotent startup seeder: system permissions/roles/packages always, plus (outside
/// Production) a demo tenant with a full day of realistic POS/kitchen/CRM/inventory activity so the
/// product can be explored/presented immediately. See README.md "Seed data / sample logins".</summary>
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
            logger.LogInformation("Seeded demo tenant 'Bella Pizza' with a full day of sample activity");
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
        db.UserRoles.Add(new UserRole(ownerUser.Id, roles["Owner"].Id, tenant.Id, locationId: null));

        var subscription = new TenantSubscription(tenant.Id, (await db.Packages.SingleAsync(p => p.Name == "Professional", ct)).Id, BillingCycle.Monthly);
        subscription.Activate("cus_demo", "sub_demo", DateTimeOffset.UtcNow.AddMonths(1));
        db.TenantSubscriptions.Add(subscription);

        var restaurant = tenant.AddRestaurant("Bella Pizza", "Bella Pizza LLC", "USD");
        db.Restaurants.Add(restaurant);

        var downtown = restaurant.AddLocation("Downtown", "123 Main St", "Springfield", "USA", "USD");
        var uptown = restaurant.AddLocation("Uptown", "456 Elm St", "Springfield", "USA", "USD");
        db.Locations.AddRange(downtown, uptown);
        var taxConfig = new TaxConfig(tenant.Id, downtown.Id, 8.25m, "Sales Tax");
        downtown.SetTaxConfig(taxConfig);
        db.Set<TaxConfig>().Add(taxConfig);

        var kitchenDept = new Department(tenant.Id, downtown.Id, "Kitchen");
        var floorDept = new Department(tenant.Id, downtown.Id, "Front of House");
        db.Departments.AddRange(kitchenDept, floorDept);

        // Tables with a simple grid layout
        var tables = new List<Table>();
        for (var i = 1; i <= 8; i++)
        {
            var table = new Table(tenant.Id, downtown.Id, $"T{i}", capacity: i % 2 == 0 ? 4 : 2, TableShape.Round, x: (i % 4) * 120, y: (i / 4) * 120);
            tables.Add(table);
            db.Tables.Add(table);
        }

        // Flush the tenant/subscription/restaurant/locations/departments/tables graph before the next
        // Identity call: UserManager.CreateAsync/UpdateAsync auto-saves the DbContext internally, which
        // would otherwise also try to flush this whole still-pending graph as a side effect.
        await db.SaveChangesAsync(ct);

        // ---- Staff ----
        var managerUser = await CreateUserAsync(userManager, "manager@bellapizza.demo", "Manager!2026", "Maria", "Manager");
        var waiterUser = await CreateUserAsync(userManager, "waiter@bellapizza.demo", "Waiter!2026", "Will", "Waiter");
        var chefUser = await CreateUserAsync(userManager, "chef@bellapizza.demo", "ChefUser!2026", "Carlos", "Chef");

        foreach (var (user, roleName) in new[] { (managerUser, "Manager"), (waiterUser, "Waiter"), (chefUser, "Chef") })
        {
            user.TenantId = tenant.Id;
            user.DefaultLocationId = downtown.Id;
            await userManager.UpdateAsync(user);
            db.UserRoles.Add(new UserRole(user.Id, roles[roleName].Id, tenant.Id, downtown.Id));
        }

        var manager = new Employee(tenant.Id, floorDept.Id, downtown.Id, managerUser.Id, "Maria", "Manager", "General Manager", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), null);
        var waiter = new Employee(tenant.Id, floorDept.Id, downtown.Id, waiterUser.Id, "Will", "Waiter", "Waiter", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-8)), 16.50m);
        var chef = new Employee(tenant.Id, kitchenDept.Id, downtown.Id, chefUser.Id, "Carlos", "Chef", "Head Chef", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)), 24.00m);
        db.Employees.AddRange(manager, waiter, chef);

        var menu = SeedMenu(db, tenant.Id, downtown.Id);
        SeedInventory(db, tenant.Id, downtown.Id);
        SeedCrm(db, tenant.Id);
        SeedOrdersAndKitchen(db, tenant.Id, downtown.Id, tables, waiter.Id, manager.Id, menu, "USD");
        SeedSalesHistory(db, tenant.Id, downtown.Id);

        _ = uptown; // reserved for future multi-location demo data (Phase 2 seed expansion)
    }

    /// <summary>Menu covering pizza/pasta/salads/desserts/drinks, with variants and modifiers, so the
    /// menu screens and POS product grid have real breadth to demo.</summary>
    private static MenuCatalog SeedMenu(ApplicationDbContext db, Guid tenantId, Guid locationId)
    {
        var pizza = new MenuCategory(tenantId, locationId, "Pizza", 1);
        var margherita = pizza.AddProduct("Margherita", "Tomato, mozzarella, basil", Money.Of(12.50m, "USD"));
        margherita.AddVariant("Large", 4.00m);
        var margheritaToppings = margherita.AddModifierGroup("Extra Toppings", isRequired: false, maxSelections: 5);
        margheritaToppings.AddModifier("Extra Cheese", 1.50m);
        margheritaToppings.AddModifier("Mushrooms", 1.00m);

        var pepperoni = pizza.AddProduct("Pepperoni", "Tomato, mozzarella, pepperoni", Money.Of(14.00m, "USD"));
        pepperoni.AddVariant("Large", 4.00m);

        var quattroFormaggi = pizza.AddProduct("Quattro Formaggi", "Mozzarella, gorgonzola, parmesan, fontina", Money.Of(16.00m, "USD"));
        quattroFormaggi.AddVariant("Large", 4.50m);

        var diavola = pizza.AddProduct("Diavola", "Tomato, mozzarella, spicy salami, chili", Money.Of(15.50m, "USD"));
        diavola.AddVariant("Large", 4.50m);

        var vegetarian = pizza.AddProduct("Vegetarian", "Tomato, mozzarella, peppers, olives, artichoke", Money.Of(14.50m, "USD"));
        vegetarian.AddVariant("Large", 4.00m);

        var pasta = new MenuCategory(tenantId, locationId, "Pasta", 2);
        var carbonara = pasta.AddProduct("Spaghetti Carbonara", "Egg, pancetta, parmesan, black pepper", Money.Of(13.00m, "USD"));
        var alfredo = pasta.AddProduct("Fettuccine Alfredo", "Butter, parmesan cream sauce", Money.Of(13.50m, "USD"));
        var arrabbiata = pasta.AddProduct("Penne Arrabbiata", "Spicy tomato, garlic, chili", Money.Of(12.00m, "USD"));

        var salads = new MenuCategory(tenantId, locationId, "Salads", 3);
        var caesar = salads.AddProduct("Caesar Salad", "Romaine, parmesan, croutons, caesar dressing", Money.Of(9.50m, "USD"));
        var greek = salads.AddProduct("Greek Salad", "Tomato, cucumber, feta, olives, oregano", Money.Of(8.50m, "USD"));

        var desserts = new MenuCategory(tenantId, locationId, "Desserts", 4);
        var tiramisu = desserts.AddProduct("Tiramisu", "Mascarpone, espresso, cocoa", Money.Of(6.50m, "USD"));
        var pannaCotta = desserts.AddProduct("Panna Cotta", "Vanilla cream, berry coulis", Money.Of(5.50m, "USD"));

        var drinks = new MenuCategory(tenantId, locationId, "Drinks", 5);
        var soda = drinks.AddProduct("Soda", "Assorted", Money.Of(3.00m, "USD"));
        var stillWater = drinks.AddProduct("Still Water", "500ml", Money.Of(2.00m, "USD"));
        var sparklingWater = drinks.AddProduct("Sparkling Water", "500ml", Money.Of(2.50m, "USD"));
        var houseWine = drinks.AddProduct("House Wine", "Glass, red or white", Money.Of(7.00m, "USD"));
        var craftBeer = drinks.AddProduct("Craft Beer", "Local IPA, 330ml", Money.Of(6.00m, "USD"));

        db.MenuCategories.AddRange(pizza, pasta, salads, desserts, drinks);

        return new MenuCatalog(margherita, pepperoni, quattroFormaggi, diavola, vegetarian,
            carbonara, alfredo, arrabbiata, caesar, greek, tiramisu, pannaCotta,
            soda, stillWater, sparklingWater, houseWine, craftBeer);
    }

    private sealed record MenuCatalog(
        Product Margherita, Product Pepperoni, Product QuattroFormaggi, Product Diavola, Product Vegetarian,
        Product Carbonara, Product Alfredo, Product Arrabbiata, Product Caesar, Product Greek,
        Product Tiramisu, Product PannaCotta,
        Product Soda, Product StillWater, Product SparklingWater, Product HouseWine, Product CraftBeer);

    /// <summary>One ingredient (Parmesan) is deliberately left below its reorder threshold so the
    /// Dashboard's low-stock alert widget has something to show out of the box.</summary>
    private static void SeedInventory(ApplicationDbContext db, Guid tenantId, Guid locationId)
    {
        var warehouse = new Warehouse(tenantId, locationId, "Main Storage");
        db.Warehouses.Add(warehouse);

        var ingredients = new[]
        {
            new Ingredient(tenantId, "Flour", MeasurementUnit.Kilogram, reorderThreshold: 10m, costPerUnit: 0.80m),
            new Ingredient(tenantId, "Mozzarella", MeasurementUnit.Kilogram, reorderThreshold: 5m, costPerUnit: 6.50m),
            new Ingredient(tenantId, "Tomato Sauce", MeasurementUnit.Liter, reorderThreshold: 8m, costPerUnit: 2.20m),
            new Ingredient(tenantId, "Pepperoni", MeasurementUnit.Kilogram, reorderThreshold: 4m, costPerUnit: 9.00m),
            new Ingredient(tenantId, "Olive Oil", MeasurementUnit.Liter, reorderThreshold: 3m, costPerUnit: 5.50m),
            new Ingredient(tenantId, "Basil", MeasurementUnit.Gram, reorderThreshold: 200m, costPerUnit: 0.03m),
            new Ingredient(tenantId, "Spaghetti", MeasurementUnit.Kilogram, reorderThreshold: 6m, costPerUnit: 1.60m),
            new Ingredient(tenantId, "Parmesan", MeasurementUnit.Kilogram, reorderThreshold: 4m, costPerUnit: 12.00m),
            new Ingredient(tenantId, "Eggs", MeasurementUnit.Piece, reorderThreshold: 24m, costPerUnit: 0.25m),
            new Ingredient(tenantId, "Romaine Lettuce", MeasurementUnit.Piece, reorderThreshold: 5m, costPerUnit: 1.10m),
        };
        db.Ingredients.AddRange(ingredients);

        var stockPlan = new (Ingredient Ingredient, decimal Quantity, int ExpiresInDays)[]
        {
            (ingredients[0], 50m, 180), // Flour
            (ingredients[1], 15m, 14),  // Mozzarella
            (ingredients[2], 20m, 90),  // Tomato Sauce
            (ingredients[3], 6m, 21),   // Pepperoni
            (ingredients[4], 10m, 365), // Olive Oil
            (ingredients[5], 300m, 5),  // Basil
            (ingredients[6], 25m, 270), // Spaghetti
            (ingredients[7], 2m, 60),   // Parmesan — deliberately below its 4kg reorder threshold
            (ingredients[8], 60m, 21),  // Eggs
            (ingredients[9], 8m, 4),    // Romaine Lettuce
        };

        foreach (var (ingredient, quantity, expiresInDays) in stockPlan)
        {
            var stock = new StockLevel(tenantId, warehouse.Id, ingredient.Id);
            stock.Receive(quantity, expiresAt: DateTimeOffset.UtcNow.AddDays(expiresInDays), unitCost: ingredient.CostPerUnit);
            db.StockLevels.Add(stock);
        }
    }

    private static void SeedCrm(ApplicationDbContext db, Guid tenantId)
    {
        var customers = new[]
        {
            new Customer(tenantId, "Sophia", "Turner", "sophia.turner@example.com", "+1-555-0101", new DateOnly(1990, 4, 12)),
            new Customer(tenantId, "James", "Miller", "james.miller@example.com", "+1-555-0102", new DateOnly(1985, 11, 3)),
            new Customer(tenantId, "Olivia", "Davis", "olivia.davis@example.com", "+1-555-0103", null),
            new Customer(tenantId, "Liam", "Garcia", "liam.garcia@example.com", "+1-555-0104", new DateOnly(1998, 7, 22)),
            new Customer(tenantId, "Emma", "Wilson", "emma.wilson@example.com", "+1-555-0105", new DateOnly(1992, 1, 30)),
        };
        db.Customers.AddRange(customers);

        var loyaltyPlan = new (int Points, string Tier)[] { (420, "Gold"), (180, "Silver"), (30, "Bronze"), (950, "Gold"), (60, "Bronze") };
        for (var i = 0; i < customers.Length; i++)
        {
            var account = new LoyaltyAccount(tenantId, customers[i].Id);
            account.Earn(loyaltyPlan[i].Points, "Historical dining rewards");
            account.SetTier(loyaltyPlan[i].Tier);
            db.LoyaltyAccounts.Add(account);
        }

        db.Coupons.Add(new Coupon(tenantId, "WELCOME10", discountPercent: 10m,
            validFrom: DateTimeOffset.UtcNow.AddMonths(-1), validTo: DateTimeOffset.UtcNow.AddMonths(2), maxRedemptions: 200));

        db.GiftCards.Add(new GiftCard(tenantId, "GIFT-BELLA-25", initialBalance: 25m, ownerCustomerId: customers[0].Id));

        db.Set<Feedback>().AddRange(
            new Feedback(tenantId, customers[0].Id, orderId: null, rating: 5, comment: "Best pizza in town, the Diavola is incredible!"),
            new Feedback(tenantId, customers[1].Id, orderId: null, rating: 4, comment: "Great food, service was a little slow at peak hours."));
    }

    /// <summary>Ten paid orders spread across today (revenue/POS history) plus five still-open orders
    /// sitting in different kitchen states, so the Dashboard, Kitchen Display, and Table map all show
    /// live, non-empty data immediately after seeding.</summary>
    private static void SeedOrdersAndKitchen(ApplicationDbContext db, Guid tenantId, Guid locationId,
        List<Table> tables, Guid waiterId, Guid managerId, MenuCatalog menu, string currency)
    {
        const decimal taxRate = 8.25m;
        Money UnitPrice(Product product, ProductVariant variant) => Money.Of(product.BasePrice.Amount + variant.PriceDelta, product.BasePrice.Currency);
        ProductVariant Regular(Product product) => product.Variants.First(v => v.IsDefault);
        ProductVariant Large(Product product) => product.Variants.First(v => v.Name == "Large");

        Order NewOrder(Guid tableId, Guid serverId) => new(tenantId, locationId, tableId, serverId, currency, taxRate);
        OrderItem AddLine(Order order, Product product, ProductVariant variant, int qty) =>
            order.AddItem(variant.Id, product.Name, variant.Name, UnitPrice(product, variant), qty, notes: null);

        // ---- Closed & paid orders (drive Today's Revenue / Order Count / AOV on the Dashboard) ----

        var o1 = NewOrder(tables[0].Id, waiterId);
        AddLine(o1, menu.Margherita, Regular(menu.Margherita), 2);
        AddLine(o1, menu.Soda, Regular(menu.Soda), 1);
        o1.SendToKitchen();
        o1.MarkReadyToServe();
        o1.MarkServed();
        o1.Pay(PaymentMethod.Cash, o1.GrandTotal, reference: null);

        var o2 = NewOrder(tables[1].Id, waiterId);
        AddLine(o2, menu.Pepperoni, Large(menu.Pepperoni), 1);
        AddLine(o2, menu.CraftBeer, Regular(menu.CraftBeer), 2);
        o2.SendToKitchen();
        o2.MarkReadyToServe();
        o2.MarkServed();
        o2.AddTip(3.00m);
        o2.Pay(PaymentMethod.Card, o2.GrandTotal, reference: "auth_5F21A");

        var o3 = NewOrder(tables[2].Id, managerId);
        AddLine(o3, menu.QuattroFormaggi, Regular(menu.QuattroFormaggi), 1);
        AddLine(o3, menu.Caesar, Regular(menu.Caesar), 1);
        AddLine(o3, menu.StillWater, Regular(menu.StillWater), 2);
        o3.SendToKitchen();
        o3.MarkReadyToServe();
        o3.MarkServed();
        o3.Pay(PaymentMethod.Cash, o3.GrandTotal, reference: null);

        var o4 = NewOrder(tables[3].Id, waiterId);
        AddLine(o4, menu.Carbonara, Regular(menu.Carbonara), 2);
        AddLine(o4, menu.HouseWine, Regular(menu.HouseWine), 1);
        o4.SendToKitchen();
        o4.MarkReadyToServe();
        o4.MarkServed();
        o4.Pay(PaymentMethod.Card, o4.GrandTotal, reference: "auth_9K02C");

        var o5 = new Order(tenantId, locationId, null, waiterId, currency, taxRate, OrderSource.QrSelfOrder);
        AddLine(o5, menu.Margherita, Regular(menu.Margherita), 1);
        AddLine(o5, menu.Tiramisu, Regular(menu.Tiramisu), 1);
        o5.SendToKitchen();
        o5.MarkReadyToServe();
        o5.MarkServed();
        o5.Pay(PaymentMethod.Card, o5.GrandTotal, reference: "auth_3B77E");

        var o6 = NewOrder(tables[4].Id, managerId);
        AddLine(o6, menu.Diavola, Large(menu.Diavola), 1);
        AddLine(o6, menu.Alfredo, Regular(menu.Alfredo), 1);
        o6.ApplyDiscount(DiscountType.FixedAmount, 5.00m, "Loyalty promo", managerId);
        o6.SendToKitchen();
        o6.MarkReadyToServe();
        o6.MarkServed();
        o6.Pay(PaymentMethod.Cash, o6.GrandTotal, reference: null);

        var o7 = NewOrder(tables[5].Id, waiterId);
        AddLine(o7, menu.Arrabbiata, Regular(menu.Arrabbiata), 3);
        AddLine(o7, menu.SparklingWater, Regular(menu.SparklingWater), 3);
        o7.SendToKitchen();
        o7.MarkReadyToServe();
        o7.MarkServed();
        o7.Pay(PaymentMethod.Cash, o7.GrandTotal, reference: null);

        var o8 = NewOrder(tables[6].Id, waiterId);
        AddLine(o8, menu.Vegetarian, Regular(menu.Vegetarian), 1);
        AddLine(o8, menu.Greek, Regular(menu.Greek), 1);
        AddLine(o8, menu.PannaCotta, Regular(menu.PannaCotta), 1);
        o8.SendToKitchen();
        o8.MarkReadyToServe();
        o8.MarkServed();
        o8.AddTip(2.50m);
        o8.Pay(PaymentMethod.Card, o8.GrandTotal, reference: "auth_1Q88F");

        var o9 = NewOrder(tables[7].Id, waiterId);
        AddLine(o9, menu.Pepperoni, Regular(menu.Pepperoni), 2);
        AddLine(o9, menu.Soda, Regular(menu.Soda), 2);
        o9.SendToKitchen();
        o9.MarkReadyToServe();
        o9.MarkServed();
        o9.Pay(PaymentMethod.Cash, 15.00m, reference: null); // split payment: cash portion...
        o9.Pay(PaymentMethod.Card, o9.AmountDue, reference: "auth_7Z10G"); // ...then the remainder on card

        var o10 = new Order(tenantId, locationId, null, managerId, currency, taxRate, OrderSource.ThirdPartyDelivery);
        o10.AttachDeliverySource(DeliveryPlatform.DoorDash, "DD-EXT-88213");
        AddLine(o10, menu.Margherita, Regular(menu.Margherita), 1);
        AddLine(o10, menu.QuattroFormaggi, Regular(menu.QuattroFormaggi), 1);
        o10.SendToKitchen();
        o10.MarkReadyToServe();
        o10.MarkServed();
        o10.Pay(PaymentMethod.Card, o10.GrandTotal, reference: "auth_4M55H");

        db.Orders.AddRange(o1, o2, o3, o4, o5, o6, o7, o8, o9, o10);

        // ---- Still-open orders in different kitchen states, so the Kitchen Display / Dashboard
        // kitchen-status widget shows one of each: queued, in-progress, ready, and overdue. ----

        var openA = NewOrder(tables[0].Id, waiterId); // browsing the menu, not sent to kitchen yet
        AddLine(openA, menu.Margherita, Regular(menu.Margherita), 1);
        AddLine(openA, menu.Soda, Regular(menu.Soda), 1);
        tables[0].Occupy();
        db.Orders.Add(openA);

        var openB = NewOrder(tables[2].Id, managerId);
        var openBLine = AddLine(openB, menu.Carbonara, Regular(menu.Carbonara), 2);
        openB.SendToKitchen();
        tables[2].Occupy();
        var ticketB = new KitchenTicket(tenantId, openB.Id, locationId, tables[2].Label, targetCookMinutes: 15);
        ticketB.AddItem(openBLine.Id, menu.Carbonara.Name, Regular(menu.Carbonara).Name, 2, notes: null);
        ticketB.Start();
        db.Orders.Add(openB);
        db.KitchenTickets.Add(ticketB);

        var openC = NewOrder(tables[4].Id, waiterId);
        var openCLine = AddLine(openC, menu.Pepperoni, Regular(menu.Pepperoni), 2);
        openC.SendToKitchen();
        tables[4].Occupy();
        var ticketC = new KitchenTicket(tenantId, openC.Id, locationId, tables[4].Label, targetCookMinutes: 12);
        ticketC.AddItem(openCLine.Id, menu.Pepperoni.Name, Regular(menu.Pepperoni).Name, 2, notes: null);
        ticketC.Start();
        ticketC.MarkReady();
        db.Orders.Add(openC);
        db.KitchenTickets.Add(ticketC);

        var openD = NewOrder(tables[6].Id, waiterId);
        var openDLine = AddLine(openD, menu.Diavola, Large(menu.Diavola), 1);
        openD.SendToKitchen();
        tables[6].Occupy();
        // TargetCookMinutes: 0 so it reads as overdue immediately once started — demos the "Overdue" chip.
        var ticketD = new KitchenTicket(tenantId, openD.Id, locationId, tables[6].Label, targetCookMinutes: 0);
        ticketD.AddItem(openDLine.Id, menu.Diavola.Name, Large(menu.Diavola).Name, 1, notes: null);
        ticketD.Start();
        db.Orders.Add(openD);
        db.KitchenTickets.Add(ticketD);

        var openE = NewOrder(tables[1].Id, waiterId);
        var openELine = AddLine(openE, menu.Arrabbiata, Regular(menu.Arrabbiata), 1);
        openE.SendToKitchen();
        tables[1].Occupy();
        var ticketE = new KitchenTicket(tenantId, openE.Id, locationId, tables[1].Label, targetCookMinutes: 10);
        ticketE.AddItem(openELine.Id, menu.Arrabbiata.Name, Regular(menu.Arrabbiata).Name, 1, notes: null);
        // left Queued — not started
        db.Orders.Add(openE);
        db.KitchenTickets.Add(ticketE);
    }

    /// <summary>Six days of materialized sales history (yesterday back) so the Dashboard's "Last 7 Days
    /// Revenue" reads as a real trend instead of just today's live total.</summary>
    private static void SeedSalesHistory(ApplicationDbContext db, Guid tenantId, Guid locationId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dailyRevenue = new decimal[] { 410m, 585m, 392m, 640m, 505m, 470m };

        for (var i = 0; i < dailyRevenue.Length; i++)
        {
            var date = today.AddDays(-(i + 1));
            var gross = dailyRevenue[i];
            var discounts = Math.Round(gross * 0.03m, 2);
            var net = gross - discounts;
            var tax = Math.Round(net * 0.0825m, 2);
            var orderCount = 14 + i * 2;

            db.Set<DailySalesSummary>().Add(new DailySalesSummary(
                tenantId, locationId, date,
                grossRevenue: gross, netRevenue: net, taxCollected: tax,
                discountsGiven: discounts, refundsIssued: 0m, orderCount: orderCount, coverCount: orderCount * 2));
        }
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
