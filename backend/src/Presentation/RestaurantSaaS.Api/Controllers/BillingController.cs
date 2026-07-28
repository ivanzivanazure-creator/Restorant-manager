using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Billing;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Infrastructure.Services;
using Stripe;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/billing")]
public sealed class BillingController(ISender mediator, Microsoft.Extensions.Options.IOptions<StripeOptions> stripeOptions) : ApiControllerBase(mediator)
{
    [HttpPost("connect-stripe")]
    [Authorize(Policy = Permissions.Billing.Manage)]
    public async Task<ActionResult<ConnectStripeAccountResultDto>> ConnectStripe(ConnectStripeRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new ConnectStripeAccountCommand(TenantId, body.ReturnUrl, body.RefreshUrl), ct));

    [HttpGet("stripe-status")]
    [Authorize(Policy = Permissions.Billing.View)]
    public async Task<ActionResult<StripeAccountStatusDto>> GetStripeStatus(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetStripeAccountStatusQuery(TenantId), ct));

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.Billing.View)]
    public async Task<ActionResult<BillingSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetBillingSummaryQuery(TenantId), ct));

    /// <summary>Stripe Connect webhook (account.updated, etc.) — authenticated by Stripe's own signature
    /// scheme, not a JWT, since the caller is Stripe's servers. See
    /// https://stripe.com/docs/webhooks/signatures for the verification this mirrors.</summary>
    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], stripeOptions.Value.WebhookSecret);
        }
        catch (StripeException)
        {
            return BadRequest("Invalid Stripe webhook signature.");
        }

        if (stripeEvent.Type == "account.updated" && stripeEvent.Data.Object is Account { DetailsSubmitted: true, ChargesEnabled: true } account)
        {
            await Mediator.Send(new MarkStripeOnboardingCompleteCommand(account.Id), ct);
        }

        return Ok();
    }
}

public sealed record ConnectStripeRequest(string ReturnUrl, string RefreshUrl);
