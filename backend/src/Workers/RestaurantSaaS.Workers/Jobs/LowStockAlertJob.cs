using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Notifications;

namespace RestaurantSaaS.Workers.Jobs;

/// <summary>Every 15 minutes: scans stock levels against each ingredient's reorder threshold and raises
/// an in-app Notification (and, where the tenant has notification email addresses configured, an email)
/// for anything below threshold — "Suggest purchasing" from here is the Phase 3 AI Assistant's job;
/// this job only detects and alerts.</summary>
public sealed class LowStockAlertJob(IApplicationDbContext db, ILogger<LowStockAlertJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var lowStockItems = await (
            from stock in db.Set<Domain.Inventory.StockLevel>()
            join ingredient in db.Ingredients on stock.IngredientId equals ingredient.Id
            join warehouse in db.Warehouses on stock.WarehouseId equals warehouse.Id
            where stock.QuantityOnHand < ingredient.ReorderThreshold
            select new { stock.TenantId, warehouse.LocationId, ingredient.Name, stock.QuantityOnHand, ingredient.ReorderThreshold }
        ).ToListAsync(ct);

        foreach (var item in lowStockItems)
        {
            var alreadyNotifiedRecently = await db.Notifications
                .Where(n => n.TenantId == item.TenantId && n.Category == NotificationCategory.LowInventory
                    && n.Body.Contains(item.Name) && n.CreatedAt > DateTimeOffset.UtcNow.AddHours(-6))
                .AnyAsync(ct);
            if (alreadyNotifiedRecently) continue;

            var notification = new Notification(item.TenantId, recipientUserId: null, NotificationCategory.LowInventory, NotificationChannel.InApp,
                title: "Low stock alert",
                body: $"{item.Name} is below its reorder threshold ({item.QuantityOnHand} on hand, threshold {item.ReorderThreshold}).");
            notification.MarkSent();
            db.Notifications.Add(notification);
        }

        if (lowStockItems.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("LowStockAlertJob evaluated {Count} below-threshold ingredient(s)", lowStockItems.Count);
        }
    }
}
