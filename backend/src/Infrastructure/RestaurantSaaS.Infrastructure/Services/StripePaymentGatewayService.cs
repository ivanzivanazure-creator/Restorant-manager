using Microsoft.Extensions.Options;
using RestaurantSaaS.Application.Common.Interfaces;
using Stripe;

namespace RestaurantSaaS.Infrastructure.Services;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";
    public string SecretKey { get; set; } = default!;
    public string WebhookSecret { get; set; } = default!;
}

/// <summary>Thin adapter over Stripe.net so Application code (subscription activation, billing) never
/// takes a direct Stripe dependency. Wire a real price catalogue via Package.FeatureFlags/Stripe price IDs
/// before going live; this class assumes StripeConfiguration.ApiKey is set once at startup (see
/// InfrastructureServiceCollectionExtensions.AddInfrastructure).</summary>
public sealed class StripePaymentGatewayService(IOptions<StripeOptions> options) : IPaymentGatewayService
{
    public async Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions { Email = email, Name = name }, cancellationToken: ct);
        return customer.Id;
    }

    public async Task<string> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
        }, cancellationToken: ct);
        return subscription.Id;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new SubscriptionService();
        await service.CancelAsync(subscriptionId, cancellationToken: ct);
    }

    public async Task<string> CreateConnectedAccountAsync(string tenantContactEmail, string companyName, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new AccountService();
        var account = await service.CreateAsync(new AccountCreateOptions
        {
            Type = "express",
            Email = tenantContactEmail,
            BusinessProfile = new AccountBusinessProfileOptions { Name = companyName },
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
            },
        }, cancellationToken: ct);
        return account.Id;
    }

    public async Task<string> CreateAccountOnboardingLinkAsync(string connectedAccountId, string returnUrl, string refreshUrl, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new AccountLinkService();
        var link = await service.CreateAsync(new AccountLinkCreateOptions
        {
            Account = connectedAccountId,
            Type = "account_onboarding",
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl,
        }, cancellationToken: ct);
        return link.Url;
    }

    public async Task<string> CapturePaymentWithApplicationFeeAsync(
        string connectedAccountId, decimal amount, string currency, decimal applicationFeeAmount, CancellationToken ct)
    {
        // NOTE: this method is wired up for a future tokenized checkout (QR self-order + Stripe Elements,
        // or Stripe Terminal for card-present POS) — see docs/ROADMAP.md. Today, in-person card payments
        // are assumed to be captured by the restaurant's own card terminal outside this system, so
        // PayOrderCommandHandler only records a PlatformFeeLedgerEntry (the fee the platform will deduct
        // at payout reconciliation) and does not call this method. It's implemented so that path is a
        // small addition, not a redesign, once a real PaymentMethod token is available to pass in.
        StripeConfiguration.ApiKey = options.Value.SecretKey;
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = ToMinorUnits(amount),
            Currency = currency.ToLowerInvariant(),
            ApplicationFeeAmount = ToMinorUnits(applicationFeeAmount),
            TransferData = new PaymentIntentTransferDataOptions { Destination = connectedAccountId },
        }, cancellationToken: ct);
        return intent.Id;
    }

    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
