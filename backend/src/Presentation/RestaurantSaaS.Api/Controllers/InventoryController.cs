using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Inventory;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/inventory")]
[Authorize(Policy = Permissions.Inventory.View)]
public sealed class InventoryController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpPost("locations/{locationId:guid}/warehouses")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<ActionResult<Guid>> CreateWarehouse(Guid locationId, [FromQuery] string name, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateWarehouseCommand(TenantId, locationId, name), ct));

    [HttpPost("ingredients")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<ActionResult<Guid>> CreateIngredient(CreateIngredientRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateIngredientCommand(TenantId, body.Name, body.Unit, body.ReorderThreshold, body.CostPerUnit, body.Barcode), ct));

    [HttpGet("warehouses/{warehouseId:guid}/stock")]
    public async Task<ActionResult<IReadOnlyCollection<StockLevelDto>>> GetStock(Guid warehouseId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetStockLevelsQuery(TenantId, warehouseId), ct));

    [HttpGet("alerts/low-stock")]
    public async Task<ActionResult<IReadOnlyCollection<StockLevelDto>>> GetLowStockAlerts(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetLowStockAlertsQuery(TenantId), ct));

    [HttpPost("stock/receive")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<IActionResult> ReceiveStock(ReceiveStockRequest body, CancellationToken ct)
    {
        await Mediator.Send(new ReceiveStockCommand(TenantId, body.WarehouseId, body.IngredientId, body.Quantity, body.UnitCost, body.ExpiresAt, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPost("stock/adjust")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<IActionResult> AdjustStock(AdjustStockRequest body, CancellationToken ct)
    {
        await Mediator.Send(new AdjustStockCommand(TenantId, body.WarehouseId, body.IngredientId, body.Delta, body.Reason, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPost("stock/transfer")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<IActionResult> TransferStock(TransferStockRequest body, CancellationToken ct)
    {
        await Mediator.Send(new TransferStockCommand(TenantId, body.SourceWarehouseId, body.DestinationWarehouseId, body.IngredientId, body.Quantity, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPost("stock/waste")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<IActionResult> RecordWaste(RecordWasteRequest body, CancellationToken ct)
    {
        await Mediator.Send(new RecordWasteCommand(TenantId, body.WarehouseId, body.IngredientId, body.Quantity, body.Reason, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPost("purchase-orders")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<ActionResult<Guid>> CreatePurchaseOrder(CreatePurchaseOrderRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreatePurchaseOrderCommand(TenantId, body.SupplierId, body.WarehouseId, CurrentUserId, body.Lines), ct));

    [HttpPost("purchase-orders/{purchaseOrderId:guid}/approve")]
    [Authorize(Policy = Permissions.Inventory.ApproveProcurement)]
    public async Task<IActionResult> ApprovePurchaseOrder(Guid purchaseOrderId, CancellationToken ct)
    {
        await Mediator.Send(new ApprovePurchaseOrderCommand(TenantId, purchaseOrderId, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPost("purchase-orders/{purchaseOrderId:guid}/receive")]
    [Authorize(Policy = Permissions.Inventory.Manage)]
    public async Task<IActionResult> ReceivePurchaseOrder(Guid purchaseOrderId, IReadOnlyCollection<ReceivedLineRequest> lines, CancellationToken ct)
    {
        await Mediator.Send(new ReceivePurchaseOrderCommand(TenantId, purchaseOrderId, CurrentUserId,
            lines.Select(l => (l.LineId, l.Quantity)).ToList()), ct);
        return NoContent();
    }
}

public sealed record CreateIngredientRequest(string Name, MeasurementUnit Unit, decimal ReorderThreshold, decimal CostPerUnit, string? Barcode);
public sealed record ReceiveStockRequest(Guid WarehouseId, Guid IngredientId, decimal Quantity, decimal UnitCost, DateTimeOffset? ExpiresAt);
public sealed record AdjustStockRequest(Guid WarehouseId, Guid IngredientId, decimal Delta, string Reason);
public sealed record TransferStockRequest(Guid SourceWarehouseId, Guid DestinationWarehouseId, Guid IngredientId, decimal Quantity);
public sealed record RecordWasteRequest(Guid WarehouseId, Guid IngredientId, decimal Quantity, string Reason);
public sealed record CreatePurchaseOrderRequest(Guid SupplierId, Guid WarehouseId, IReadOnlyCollection<PurchaseOrderLineInput> Lines);
public sealed record ReceivedLineRequest(Guid LineId, decimal Quantity);
