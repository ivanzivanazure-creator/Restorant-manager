using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantSaaS.Application.Pos;
using RestaurantSaaS.Domain.Enums;
using Xunit;

namespace RestaurantSaaS.Application.UnitTests.Pos;

public class PayOrderCommandValidatorTests
{
    private readonly PayOrderCommandValidator _validator = new();

    [Fact]
    public void PositiveAmount_PassesValidation()
    {
        var command = new PayOrderCommand(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Cash, 10m, null);
        _validator.TestValidate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveAmount_FailsValidation(decimal amount)
    {
        var command = new PayOrderCommand(Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Cash, amount, null);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Amount);
    }
}

public class ApplyDiscountCommandValidatorTests
{
    private readonly ApplyDiscountCommandValidator _validator = new();

    [Fact]
    public void ZeroAmountOff_FailsValidation()
    {
        var command = new ApplyDiscountCommand(Guid.NewGuid(), Guid.NewGuid(), DiscountType.FixedAmount, 0m, "reason", Guid.NewGuid());
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.AmountOff);
    }
}

public class AddOrderItemCommandValidatorTests
{
    private readonly AddOrderItemCommandValidator _validator = new();

    [Fact]
    public void ZeroQuantity_FailsValidation()
    {
        var command = new AddOrderItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null, []);
        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Quantity);
    }
}
