using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Integrations;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/integrations")]
public sealed class IntegrationsController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpPost("delivery")]
    [Authorize(Policy = Permissions.Integrations.Manage)]
    public async Task<ActionResult<RegisterDeliveryIntegrationResponse>> Register(RegisterDeliveryIntegrationRequest body, CancellationToken ct)
    {
        var (integration, secret) = await Mediator.Send(
            new RegisterDeliveryIntegrationCommand(TenantId, body.LocationId, body.Platform, body.ExternalStoreId), ct);

        var webhookUrl = Url.Action(nameof(IngestOrder), "Integrations", new { platform = body.Platform, locationId = body.LocationId }, Request.Scheme)!;

        return Ok(new RegisterDeliveryIntegrationResponse(integration, secret, webhookUrl));
    }

    [HttpGet("delivery")]
    [Authorize(Policy = Permissions.Integrations.Manage)]
    public async Task<ActionResult<IReadOnlyCollection<DeliveryIntegrationDto>>> List(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListDeliveryIntegrationsQuery(TenantId), ct));

    [HttpPost("delivery/{integrationId:guid}/deactivate")]
    [Authorize(Policy = Permissions.Integrations.Manage)]
    public async Task<IActionResult> Deactivate(Guid integrationId, CancellationToken ct)
    {
        await Mediator.Send(new DeactivateDeliveryIntegrationCommand(TenantId, integrationId), ct);
        return NoContent();
    }

    /// <summary>Delivery-platform webhook: authenticated by a shared secret (X-Webhook-Secret header),
    /// not a JWT — the caller is UberEats/DoorDash/etc., not a logged-in staff member.</summary>
    [HttpPost("delivery/{platform}/webhook/{locationId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IngestDeliveryOrderResultDto>> IngestOrder(
        DeliveryPlatform platform, Guid locationId, [FromHeader(Name = "X-Webhook-Secret")] string webhookSecret,
        DeliveryOrderWebhookPayload body, CancellationToken ct)
    {
        var result = await Mediator.Send(new IngestDeliveryOrderCommand(locationId, platform, webhookSecret, body.ExternalOrderId, body.Items), ct);
        return Ok(result);
    }
}

public sealed record RegisterDeliveryIntegrationRequest(Guid LocationId, DeliveryPlatform Platform, string? ExternalStoreId);
public sealed record RegisterDeliveryIntegrationResponse(DeliveryIntegrationDto Integration, string WebhookSecret, string WebhookUrl);
public sealed record DeliveryOrderWebhookPayload(string ExternalOrderId, IReadOnlyCollection<DeliveryOrderItemPayload> Items);
