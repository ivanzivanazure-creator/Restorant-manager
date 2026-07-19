using FluentAssertions;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;
using RestaurantSaaS.Domain.Subscription;
using Xunit;

namespace RestaurantSaaS.Domain.UnitTests.Subscription;

public class TenantSubscriptionTests
{
    [Fact]
    public void NewSubscription_StartsInTrialingStatus()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, trialDays: 14);

        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.TrialEndsAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(14), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void TryAutoLock_BeforeTrialEnds_DoesNotLock()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, trialDays: 14);

        var locked = subscription.TryAutoLock(DateTimeOffset.UtcNow);

        locked.Should().BeFalse();
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public void TryAutoLock_AfterTrialEnds_Locks()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly, trialDays: 14);

        var locked = subscription.TryAutoLock(DateTimeOffset.UtcNow.AddDays(15));

        locked.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Locked);
    }

    [Fact]
    public void TryAutoLock_ActiveWithinGracePeriod_DoesNotLock()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly);
        subscription.Activate("cus_1", "sub_1", DateTimeOffset.UtcNow.AddDays(-1)); // period just ended

        var locked = subscription.TryAutoLock(DateTimeOffset.UtcNow); // within 3-day grace period

        locked.Should().BeFalse();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void TryAutoLock_ActivePastGracePeriod_Locks()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly);
        subscription.Activate("cus_1", "sub_1", DateTimeOffset.UtcNow.AddDays(-5));

        var locked = subscription.TryAutoLock(DateTimeOffset.UtcNow);

        locked.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Locked);
    }

    [Fact]
    public void EnsureActiveOrThrow_WhenLocked_Throws()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly);
        subscription.Activate("cus_1", "sub_1", DateTimeOffset.UtcNow.AddDays(-10));
        subscription.TryAutoLock(DateTimeOffset.UtcNow);

        var act = () => subscription.EnsureActiveOrThrow("Bella Pizza");

        act.Should().Throw<SubscriptionLockedException>();
    }

    [Fact]
    public void RenewPeriod_WhenPastDue_ReactivatesSubscription()
    {
        var subscription = new TenantSubscription(Guid.NewGuid(), Guid.NewGuid(), BillingCycle.Monthly);
        subscription.Activate("cus_1", "sub_1", DateTimeOffset.UtcNow.AddDays(-1));
        subscription.MarkPastDue();

        subscription.RenewPeriod(DateTimeOffset.UtcNow.AddMonths(1));

        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }
}
