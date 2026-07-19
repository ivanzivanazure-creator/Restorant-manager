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
}
