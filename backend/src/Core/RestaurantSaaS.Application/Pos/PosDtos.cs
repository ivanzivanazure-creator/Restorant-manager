using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Pos;

public sealed record OrderItemModifierDto(string Name, decimal PriceDelta);

public sealed record OrderItemDto(
    Guid Id, Guid ProductVariantId, string ProductName, string VariantName, decimal UnitPrice, int Quantity,
    string? Notes, decimal LineTotal, IReadOnlyCollection<OrderItemModifierDto> Modifiers);

public sealed record PaymentDto(Guid Id, PaymentMethod Method, decimal Amount, PaymentStatus Status, string? Reference);

public sealed record OrderDto(
    Guid Id, Guid LocationId, Guid? TableId, OrderStatus Status, OrderSource Source, string Currency,
    decimal Subtotal, decimal DiscountTotal, decimal TaxTotal, decimal TipAmount, decimal GrandTotal,
    decimal AmountPaid, decimal AmountDue, DateTimeOffset OpenedAt,
    IReadOnlyCollection<OrderItemDto> Items, IReadOnlyCollection<PaymentDto> Payments);
