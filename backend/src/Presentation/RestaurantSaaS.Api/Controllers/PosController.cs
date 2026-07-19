using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Pos;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/pos")]
public sealed class PosController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpPost("orders")]
    [Authorize(Policy = Permissions.Pos.OpenOrder)]
    public async Task<ActionResult<Guid>> OpenOrder(OpenOrderRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new OpenOrderCommand(TenantId, body.LocationId, body.TableId, body.ServerEmployeeId, body.Source), ct));

    [HttpGet("orders/{orderId:guid}")]
    [Authorize(Policy = Permissions.Pos.ViewOrders)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid orderId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOrderQuery(TenantId, orderId), ct));

    [HttpGet("locations/{locationId:guid}/orders/open")]
    [Authorize(Policy = Permissions.Pos.ViewOrders)]
    public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetOpenOrders(Guid locationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOpenOrdersByLocationQuery(TenantId, locationId), ct));

    [HttpPost("orders/{orderId:guid}/items")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<ActionResult<Guid>> AddItem(Guid orderId, AddOrderItemRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AddOrderItemCommand(TenantId, orderId, body.ProductVariantId, body.Quantity, body.Notes, body.Modifiers), ct));

    [HttpDelete("orders/{orderId:guid}/items/{orderItemId:guid}")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<IActionResult> RemoveItem(Guid orderId, Guid orderItemId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveOrderItemCommand(TenantId, orderId, orderItemId), ct);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/discounts")]
    [Authorize(Policy = Permissions.Pos.ApplyDiscount)]
    public async Task<IActionResult> ApplyDiscount(Guid orderId, ApplyDiscountRequest body, CancellationToken ct)
    {
        await Mediator.Send(new ApplyDiscountCommand(TenantId, orderId, body.Type, body.AmountOff, body.Reason, CurrentUserId), ct);
        return NoContent();
    }

    [HttpPut("orders/{orderId:guid}/tip")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<IActionResult> AddTip(Guid orderId, [FromQuery] decimal amount, CancellationToken ct)
    {
        await Mediator.Send(new AddTipCommand(TenantId, orderId, amount), ct);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/split")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<ActionResult<Guid>> Split(Guid orderId, IReadOnlyCollection<Guid> orderItemIds, CancellationToken ct) =>
        Ok(await Mediator.Send(new SplitOrderCommand(TenantId, orderId, orderItemIds, CurrentUserId), ct));

    [HttpPost("orders/{targetOrderId:guid}/merge/{sourceOrderId:guid}")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<IActionResult> Merge(Guid targetOrderId, Guid sourceOrderId, CancellationToken ct)
    {
        await Mediator.Send(new MergeOrdersCommand(TenantId, targetOrderId, sourceOrderId), ct);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/send-to-kitchen")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<ActionResult<Guid>> SendToKitchen(Guid orderId, [FromQuery] Guid warehouseId, [FromQuery] int targetCookMinutes, CancellationToken ct) =>
        Ok(await Mediator.Send(new SendOrderToKitchenCommand(TenantId, orderId, warehouseId, targetCookMinutes), ct));

    [HttpPut("orders/{orderId:guid}/served")]
    [Authorize(Policy = Permissions.Pos.ModifyOrder)]
    public async Task<IActionResult> MarkServed(Guid orderId, CancellationToken ct)
    {
        await Mediator.Send(new MarkOrderServedCommand(TenantId, orderId), ct);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/payments")]
    [Authorize(Policy = Permissions.Pos.TakePayment)]
    public async Task<ActionResult<PaymentDto>> Pay(Guid orderId, PayOrderRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new PayOrderCommand(TenantId, orderId, body.Method, body.Amount, body.Reference), ct));

    [HttpPost("orders/{orderId:guid}/refunds")]
    [Authorize(Policy = Permissions.Pos.IssueRefund)]
    public async Task<IActionResult> Refund(Guid orderId, RefundOrderRequest body, CancellationToken ct)
    {
        await Mediator.Send(new RefundOrderCommand(TenantId, orderId, body.Amount, body.Reason, CurrentUserId), ct);
        return NoContent();
    }
}

public sealed record OpenOrderRequest(Guid LocationId, Guid? TableId, Guid ServerEmployeeId, OrderSource Source);
public sealed record AddOrderItemRequest(Guid ProductVariantId, int Quantity, string? Notes, IReadOnlyCollection<OrderItemModifierSelection> Modifiers);
public sealed record ApplyDiscountRequest(DiscountType Type, decimal AmountOff, string Reason);
public sealed record PayOrderRequest(PaymentMethod Method, decimal Amount, string? Reference);
public sealed record RefundOrderRequest(decimal Amount, string Reason);
