using FluentAssertions;
using RestaurantSaaS.Domain.Exceptions;
using RestaurantSaaS.Domain.ValueObjects;
using Xunit;

namespace RestaurantSaaS.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_RoundsToTwoDecimalPlaces()
    {
        var money = Money.Of(12.345m, "usd");

        money.Amount.Should().Be(12.35m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WithSameCurrency_SumsAmounts()
    {
        var a = Money.Of(10m, "USD");
        var b = Money.Of(5.50m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(15.50m);
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var usd = Money.Of(10m, "USD");
        var eur = Money.Of(10m, "EUR");

        var act = () => usd.Add(eur);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        Money.Of(10m, "USD").Should().Be(Money.Of(10m, "USD"));
        Money.Of(10m, "USD").Should().NotBe(Money.Of(10m, "EUR"));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("")]
    [InlineData("USDD")]
    public void Of_WithInvalidCurrencyCode_Throws(string currency)
    {
        var act = () => Money.Of(10m, currency);
        act.Should().Throw<DomainException>();
    }
}
