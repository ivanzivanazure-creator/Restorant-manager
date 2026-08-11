using FluentAssertions;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.ValueObjects;
using Xunit;

namespace RestaurantSaaS.Domain.UnitTests.Pos;

public class OrderTests
{
    private static Order CreateOpenOrder(decimal taxRatePercent = 10m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "USD", taxRatePercent);

    [Fact]
    public void AddItem_IncreasesSubtotalByLineTotal()
    {
        var order = CreateOpenOrder();

        order.AddItem(Guid.NewGuid(), "Margherita", "Large", Money.Of(12.50m, "USD"), quantity: 2, notes: null);

        order.Subtotal.Should().Be(25.00m);
    }

    [Fact]
    public void GrandTotal_IncludesTaxOnDiscountedSubtotalPlusTip()
    {
        var order = CreateOpenOrder(taxRatePercent: 10m);
        order.AddItem(Guid.NewGuid(), "Pizza", "Regular", Money.Of(100m, "USD"), quantity: 1, notes: null);
        order.ApplyDiscount(DiscountType.FixedAmount, 20m, "loyalty", Guid.NewGuid());
        order.AddTip(5m);

        // Subtotal 100, discount 20 => taxable 80, tax 10% = 8, + tip 5 = 93
        order.GrandTotal.Should().Be(93m);
    }

    [Fact]
    public void SendToKitchen_WithNoItems_Throws()
    {
        var order = CreateOpenOrder();

        var act = order.SendToKitchen;

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Fact]
    public void Pay_WhenFullyPaid_TransitionsToPaidAndClosesOrder()
    {
        var order = CreateOpenOrder(taxRatePercent: 0m);
        order.AddItem(Guid.NewGuid(), "Soda", "Regular", Money.Of(3m, "USD"), quantity: 1, notes: null);

        order.Pay(PaymentMethod.Cash, 3m, reference: null);

        order.Status.Should().Be(OrderStatus.Paid);
        order.ClosedAt.Should().NotBeNull();
        order.AmountDue.Should().Be(0m);
    }

    [Fact]
    public void Pay_WhenPartial_TransitionsToPartiallyPaid()
    {
        var order = CreateOpenOrder(taxRatePercent: 0m);
        order.AddItem(Guid.NewGuid(), "Pizza", "Regular", Money.Of(20m, "USD"), quantity: 1, notes: null);

        order.Pay(PaymentMethod.Cash, 10m, reference: null);

        order.Status.Should().Be(OrderStatus.PartiallyPaid);
        order.AmountDue.Should().Be(10m);
    }

    [Fact]
    public void SplitOff_MovesSelectedItemsToNewOrderAndRemovesFromOriginal()
    {
        var order = CreateOpenOrder();
        var item1 = order.AddItem(Guid.NewGuid(), "Pizza", "Regular", Money.Of(10m, "USD"), 1, null);
        var item2 = order.AddItem(Guid.NewGuid(), "Salad", "Regular", Money.Of(8m, "USD"), 1, null);

        var newOrder = order.SplitOff([item1.Id], Guid.NewGuid());

        order.Items.Should().ContainSingle(i => i.Id == item2.Id);
        newOrder.Items.Should().ContainSingle(i => i.ProductName == "Pizza");
    }

    [Fact]
    public void MergeFrom_MovesAllItemsAndCancelsSourceOrder()
    {
        var target = CreateOpenOrder();
        var source = CreateOpenOrder();
        source.AddItem(Guid.NewGuid(), "Pizza", "Regular", Money.Of(10m, "USD"), 1, null);

        target.MergeFrom(source);

        target.Items.Should().ContainSingle(i => i.ProductName == "Pizza");
        source.Status.Should().Be(OrderStatus.Cancelled);
        source.Items.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_WhenAlreadyPaid_Throws()
    {
        var order = CreateOpenOrder(taxRatePercent: 0m);
        order.AddItem(Guid.NewGuid(), "Soda", "Regular", Money.Of(3m, "USD"), 1, null);
        order.Pay(PaymentMethod.Cash, 3m, null);

        var act = order.Cancel;

        act.Should().Throw<InvalidOrderStateException>();
    }
}
