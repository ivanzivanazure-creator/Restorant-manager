using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Billing;

/// <summary>One row per card payment the platform took a transaction fee on — the audit trail behind
/// the usage-based revenue line (Package.TransactionFeePercent). Written by PayOrderCommandHandler
/// whenever a Card payment is captured for a tenant with a non-zero fee rate and a connected Stripe
/// account; reconciled against Stripe's own application-fee records during payout accounting.</summary>
public class PlatformFeeLedgerEntry : TenantAuditableEntity
{
    public Guid PaymentId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal PaymentAmount { get; private set; }
    public decimal FeeRatePercent { get; private set; }
    public decimal FeeAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? StripeApplicationFeeId { get; private set; }

    private PlatformFeeLedgerEntry() { }

    public PlatformFeeLedgerEntry(Guid tenantId, Guid paymentId, Guid orderId, decimal paymentAmount,
        decimal feeRatePercent, string currency, string? stripeApplicationFeeId = null)
    {
        TenantId = tenantId;
        PaymentId = paymentId;
        OrderId = orderId;
        PaymentAmount = paymentAmount;
        FeeRatePercent = feeRatePercent;
        FeeAmount = decimal.Round(paymentAmount * feeRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
        StripeApplicationFeeId = stripeApplicationFeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AttachStripeApplicationFeeId(string stripeApplicationFeeId) => StripeApplicationFeeId = stripeApplicationFeeId;
}
