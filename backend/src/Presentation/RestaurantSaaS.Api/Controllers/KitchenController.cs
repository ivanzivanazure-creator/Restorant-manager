using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Kitchen;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/kitchen")]
public sealed class KitchenController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("locations/{locationId:guid}/queue")]
    [Authorize(Policy = Permissions.Kitchen.ViewQueue)]
    public async Task<ActionResult<IReadOnlyCollection<KitchenTicketDto>>> GetQueue(Guid locationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetKitchenQueueQuery(TenantId, locationId), ct));

    [HttpPut("tickets/{ticketId:guid}/start")]
    [Authorize(Policy = Permissions.Kitchen.ManageTickets)]
    public async Task<IActionResult> Start(Guid ticketId, CancellationToken ct)
    {
        await Mediator.Send(new StartKitchenTicketCommand(TenantId, ticketId), ct);
        return NoContent();
    }

    [HttpPut("tickets/{ticketId:guid}/ready")]
    [Authorize(Policy = Permissions.Kitchen.ManageTickets)]
    public async Task<IActionResult> MarkReady(Guid ticketId, CancellationToken ct)
    {
        await Mediator.Send(new MarkKitchenTicketReadyCommand(TenantId, ticketId), ct);
        return NoContent();
    }

    [HttpPut("tickets/{ticketId:guid}/served")]
    [Authorize(Policy = Permissions.Kitchen.ManageTickets)]
    public async Task<IActionResult> MarkServed(Guid ticketId, CancellationToken ct)
    {
        await Mediator.Send(new MarkKitchenTicketServedCommand(TenantId, ticketId), ct);
        return NoContent();
    }

    [HttpPut("tickets/{ticketId:guid}/priority")]
    [Authorize(Policy = Permissions.Kitchen.ManageTickets)]
    public async Task<IActionResult> SetPriority(Guid ticketId, [FromQuery] KitchenTicketPriority priority, CancellationToken ct)
    {
        await Mediator.Send(new SetKitchenTicketPriorityCommand(TenantId, ticketId, priority), ct);
        return NoContent();
    }
}
