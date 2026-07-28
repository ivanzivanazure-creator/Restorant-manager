using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Subscription;

/// <summary>A sellable plan, e.g. Starter (5 users), Professional (10 users), Unlimited.</summary>
public class Package : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public int? MaxUsers { get; private set; } // null = unlimited
    public int MaxLocations { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public decimal YearlyPrice { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyDictionary<string, bool> FeatureFlags { get; private set; } = new Dictionary<string, bool>();

    /// <summary>Take-rate on card payments processed through the POS for tenants on this package —
    /// the platform's usage-based revenue line, separate from (and additive to) the flat subscription
    /// price. Charged via Stripe Connect application fees; see Billing/PlatformFeeLedgerEntry.</summary>
    public decimal TransactionFeePercent { get; private set; }

    public SlaTier SlaTier { get; private set; } = SlaTier.Standard;
    public decimal SlaUptimeTargetPercent { get; private set; } = 99.5m;

    private Package() { }

    public Package(string name, int? maxUsers, int maxLocations, decimal monthlyPrice, decimal yearlyPrice,
        decimal transactionFeePercent = 0m, SlaTier slaTier = SlaTier.Standard, decimal slaUptimeTargetPercent = 99.5m,
        IReadOnlyDictionary<string, bool>? featureFlags = null)
    {
        Name = name;
        MaxUsers = maxUsers;
        MaxLocations = maxLocations;
        MonthlyPrice = monthlyPrice;
        YearlyPrice = yearlyPrice;
        TransactionFeePercent = transactionFeePercent;
        SlaTier = slaTier;
        SlaUptimeTargetPercent = slaUptimeTargetPercent;
        FeatureFlags = featureFlags ?? new Dictionary<string, bool>();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}

public class TenantSubscription : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PackageId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset TrialEndsAt { get; private set; }
    public DateTimeOffset CurrentPeriodEnd { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }

    private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(3);

    private TenantSubscription() { }

    public TenantSubscription(Guid tenantId, Guid packageId, BillingCycle billingCycle, int trialDays = 14)
    {
        TenantId = tenantId;
        PackageId = packageId;
        BillingCycle = billingCycle;
        Status = SubscriptionStatus.Trialing;
        TrialEndsAt = DateTimeOffset.UtcNow.AddDays(trialDays);
        CurrentPeriodEnd = TrialEndsAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate(string stripeCustomerId, string stripeSubscriptionId, DateTimeOffset periodEnd)
    {
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = periodEnd;
        LockedAt = null;
    }

    public void RenewPeriod(DateTimeOffset newPeriodEnd)
    {
        CurrentPeriodEnd = newPeriodEnd;
        if (Status is SubscriptionStatus.PastDue or SubscriptionStatus.Locked) Status = SubscriptionStatus.Active;
    }

    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;

    /// <summary>Called by the hourly SubscriptionExpirationJob; locks tenants past grace period.</summary>
    public bool TryAutoLock(DateTimeOffset now)
    {
        var expired = Status is SubscriptionStatus.Trialing && now > TrialEndsAt
            || Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue && now > CurrentPeriodEnd + GracePeriod;

        if (!expired) return false;

        Status = SubscriptionStatus.Locked;
        LockedAt = now;
        return true;
    }

    public void Cancel() => Status = SubscriptionStatus.Cancelled;

    public void EnsureActiveOrThrow(string tenantName)
    {
        if (Status is SubscriptionStatus.Locked or SubscriptionStatus.Cancelled)
            throw new SubscriptionLockedException(tenantName);
    }

    public void ChangePackage(Guid newPackageId) => PackageId = newPackageId;
}

public class Invoice : AuditableEntity
{
    public Guid TenantSubscriptionId { get; private set; }
    public string Number { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? StripeInvoiceId { get; private set; }
    public string? PdfUrl { get; private set; }

    private Invoice() { }

    public Invoice(Guid tenantSubscriptionId, string number, decimal amount, string currency)
    {
        TenantSubscriptionId = tenantSubscriptionId;
        Number = number;
        Amount = amount;
        Currency = currency;
        IssuedAt = DateTimeOffset.UtcNow;
        Status = InvoiceStatus.Sent;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(string? stripeInvoiceId = null)
    {
        Status = InvoiceStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        StripeInvoiceId = stripeInvoiceId ?? StripeInvoiceId;
    }

    public void MarkOverdue() => Status = InvoiceStatus.Overdue;
    public void AttachPdf(string url) => PdfUrl = url;
}
