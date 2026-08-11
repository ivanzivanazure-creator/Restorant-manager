using FluentAssertions;
using RestaurantSaaS.Domain.Exceptions;
using RestaurantSaaS.Domain.Inventory;
using Xunit;

namespace RestaurantSaaS.Domain.UnitTests.Inventory;

public class StockLevelTests
{
    private static StockLevel CreateStockLevel() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Receive_IncreasesQuantityOnHand()
    {
        var stock = CreateStockLevel();

        stock.Receive(10m, expiresAt: null, unitCost: 1m);

        stock.QuantityOnHand.Should().Be(10m);
    }

    [Fact]
    public void Consume_DepletesOldestBatchFirst_Fifo()
    {
        var stock = CreateStockLevel();
        var oldBatch = stock.Receive(5m, DateTimeOffset.UtcNow.AddDays(1), unitCost: 1m);
        var newBatch = stock.Receive(5m, DateTimeOffset.UtcNow.AddDays(10), unitCost: 1m);

        stock.Consume(7m, "Flour");

        oldBatch.RemainingQuantity.Should().Be(0m);
        newBatch.RemainingQuantity.Should().Be(3m);
        stock.QuantityOnHand.Should().Be(3m);
    }

    [Fact]
    public void Consume_PrefersEarliestExpiringBatchOverReceivedOrder()
    {
        var stock = CreateStockLevel();
        var receivedFirstButExpiresLater = stock.Receive(5m, DateTimeOffset.UtcNow.AddDays(30), unitCost: 1m);
        var receivedSecondButExpiresSoon = stock.Receive(5m, DateTimeOffset.UtcNow.AddDays(1), unitCost: 1m);

        stock.Consume(3m, "Milk");

        receivedSecondButExpiresSoon.RemainingQuantity.Should().Be(2m);
        receivedFirstButExpiresLater.RemainingQuantity.Should().Be(5m);
    }

    [Fact]
    public void Consume_MoreThanAvailable_ThrowsInsufficientStock()
    {
        var stock = CreateStockLevel();
        stock.Receive(2m, null, 1m);

        var act = () => stock.Consume(5m, "Cheese");

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void ApplyCorrection_NeverGoesNegative()
    {
        var stock = CreateStockLevel();
        stock.Receive(2m, null, 1m);

        stock.ApplyCorrection(-10m);

        stock.QuantityOnHand.Should().Be(0m);
    }

    [Fact]
    public void IsBelowReorderThreshold_ComparesAgainstGivenThreshold()
    {
        var stock = CreateStockLevel();
        stock.Receive(3m, null, 1m);

        stock.IsBelowReorderThreshold(5m).Should().BeTrue();
        stock.IsBelowReorderThreshold(1m).Should().BeFalse();
    }
}
