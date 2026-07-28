using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Recipes;

namespace RestaurantSaaS.Application.Pos;

/// <summary>Shared "send an order to the kitchen" workflow: creates the KitchenTicket, deducts recipe
/// ingredients via FIFO, flips the Order to InKitchen, and pushes the real-time KDS event. Used by both
/// the POS-initiated SendOrderToKitchenCommand and the delivery-platform webhook ingestion path
/// (Integrations/DeliveryOrderIngestion.cs) so both dispatch the same way instead of duplicating it.</summary>
public sealed class OrderKitchenDispatchService(IApplicationDbContext db, IRealtimeNotifier realtime)
{
    public async Task<KitchenTicket> DispatchAsync(Order order, Guid warehouseId, int targetCookMinutes, CancellationToken ct)
    {
        string? tableLabel = null;
        if (order.TableId is not null)
        {
            tableLabel = (await db.Tables.SingleOrDefaultAsync(t => t.Id == order.TableId, ct))?.Label;
        }

        var ticket = new KitchenTicket(order.TenantId, order.Id, order.LocationId, tableLabel, targetCookMinutes);
        foreach (var item in order.Items)
        {
            ticket.AddItem(item.Id, item.ProductName, item.VariantName, item.Quantity, item.Notes);
        }
        db.Set<KitchenTicket>().Add(ticket);

        await DeductInventoryAsync(order, warehouseId, ct);

        order.SendToKitchen();
        await db.SaveChangesAsync(ct);

        await realtime.NotifyKitchenAsync(order.LocationId, new
        {
            @event = "ticket-queued",
            ticketId = ticket.Id,
            orderId = order.Id,
            tableLabel,
            items = ticket.Items.Select(i => new { i.ProductName, i.VariantName, i.Quantity, i.Notes }),
        }, ct);

        return ticket;
    }

    private async Task DeductInventoryAsync(Order order, Guid warehouseId, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var variant = await db.Set<ProductVariant>().SingleAsync(v => v.Id == item.ProductVariantId, ct);
            var recipe = await db.Set<Recipe>().Include(r => r.Ingredients)
                .SingleOrDefaultAsync(r => r.ProductId == variant.ProductId, ct);
            if (recipe is null) continue; // no recipe modeled yet for this product — nothing to deduct

            foreach (var line in recipe.Ingredients)
            {
                var stock = await db.Set<StockLevel>().Include(s => s.Batches)
                    .SingleOrDefaultAsync(s => s.WarehouseId == warehouseId && s.IngredientId == line.IngredientId, ct);
                if (stock is null) continue;

                var ingredientName = (await db.Ingredients.SingleAsync(i => i.Id == line.IngredientId, ct)).Name;
                stock.Consume(line.Quantity * item.Quantity, ingredientName);

                db.StockMovements.Add(new Domain.Inventory.StockMovement(
                    order.TenantId, warehouseId, line.IngredientId, StockMovementType.Sale,
                    -(line.Quantity * item.Quantity), reference: order.Id.ToString(), performedByEmployeeId: order.ServerEmployeeId));
            }
        }
    }
}
