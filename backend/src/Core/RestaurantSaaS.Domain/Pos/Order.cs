using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Domain.Pos;

public class Order : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public Guid? TableId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? GuestStayId { get; private set; } // hotel room-charge orders
    public Guid ServerEmployeeId { get; private set; }
    public string Currency { get; private set; } = default!;
    public OrderStatus Status { get; private set; } = OrderStatus.Open;
    public OrderSource Source { get; private set; } = OrderSource.Pos;
    public decimal TaxRatePercent { get; private set; }
    public decimal TipAmount { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<Payment> _payments = [];
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private readonly List<DiscountApplication> _discounts = [];
    public IReadOnlyCollection<DiscountApplication> Discounts => _discounts.AsReadOnly();

    public decimal Subtotal => _items.Sum(i => i.LineTotal);
    public decimal DiscountTotal => _discounts.Sum(d => d.AmountOff);
    public decimal TaxableAmount => Math.Max(0, Subtotal - DiscountTotal);
    public decimal TaxTotal => decimal.Round(TaxableAmount * (TaxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
    public decimal GrandTotal => TaxableAmount + TaxTotal + TipAmount;
    public decimal AmountPaid => _payments.Where(p => p.Status == PaymentStatus.Captured).Sum(p => p.Amount);
    public decimal AmountDue => Math.Max(0, GrandTotal - AmountPaid);

    private Order() { }

    public Order(Guid tenantId, Guid locationId, Guid? tableId, Guid serverEmployeeId, string currency,
        decimal taxRatePercent, OrderSource source = OrderSource.Pos, Guid? customerId = null, Guid? guestStayId = null)
    {
        TenantId = tenantId;
        LocationId = locationId;
        TableId = tableId;
        ServerEmployeeId = serverEmployeeId;
        Currency = currency;
        TaxRatePercent = taxRatePercent;
        Source = source;
        CustomerId = customerId;
        GuestStayId = guestStayId;
        OpenedAt = DateTimeOffset.UtcNow;
        CreatedAt = OpenedAt;

        AddDomainEvent(new OrderPlacedEvent(Id, TenantId, LocationId));
    }

    public OrderItem AddItem(Guid productVariantId, string productName, string variantName, Money unitPrice, int quantity, string? notes)
    {
        EnsureMutable();
        var item = new OrderItem(TenantId, Id, productVariantId, productName, variantName, unitPrice, quantity, notes);
        _items.Add(item);
        AddDomainEvent(new OrderItemsAddedEvent(Id, TenantId, LocationId, [item.Id]));
        return item;
    }

    public void RemoveItem(Guid orderItemId)
    {
        EnsureMutable();
        _items.RemoveAll(i => i.Id == orderItemId);
    }

    public void ApplyDiscount(DiscountType type, decimal amountOff, string reason, Guid appliedByEmployeeId)
    {
        EnsureMutable();
        _discounts.Add(new DiscountApplication(TenantId, Id, type, amountOff, reason, appliedByEmployeeId));
    }

    public void AddTip(decimal amount)
    {
        if (amount < 0) throw new DomainException("Tip amount cannot be negative.");
        TipAmount = amount;
    }

    public void SendToKitchen()
    {
        EnsureMutable();
        if (_items.Count == 0) throw new InvalidOrderStateException("Cannot send an empty order to the kitchen.");
        Status = OrderStatus.InKitchen;
    }

    public void MarkReadyToServe()
    {
        if (Status != OrderStatus.InKitchen)
            throw new InvalidOrderStateException($"Cannot mark order ready from status {Status}.");
        Status = OrderStatus.ReadyToServe;
        AddDomainEvent(new OrderReadyToServeEvent(Id, TenantId, LocationId, TableId));
    }

    public void MarkServed()
    {
        if (Status != OrderStatus.ReadyToServe)
            throw new InvalidOrderStateException($"Cannot mark order served from status {Status}.");
        Status = OrderStatus.Served;
    }

    /// <summary>Splits the given items off into a brand-new order on the same table (split bill).</summary>
    public Order SplitOff(IEnumerable<Guid> orderItemIds, Guid requestedByEmployeeId)
    {
        var toMove = _items.Where(i => orderItemIds.Contains(i.Id)).ToList();
        if (toMove.Count == 0) throw new DomainException("No matching items to split.");

        var newOrder = new Order(TenantId, LocationId, TableId, requestedByEmployeeId, Currency, TaxRatePercent, Source, CustomerId, GuestStayId);
        foreach (var item in toMove)
        {
            newOrder.AddItem(item.ProductVariantId, item.ProductName, item.VariantName, item.UnitPrice, item.Quantity, item.Notes);
            _items.Remove(item);
        }
        return newOrder;
    }

    /// <summary>Merges another order's items into this one (merge tables) and closes the other order.</summary>
    public void MergeFrom(Order other)
    {
        EnsureMutable();
        foreach (var item in other._items.ToList())
        {
            AddItem(item.ProductVariantId, item.ProductName, item.VariantName, item.UnitPrice, item.Quantity, item.Notes);
        }
        other._items.Clear();
        other.Status = OrderStatus.Cancelled;
        other.ClosedAt = DateTimeOffset.UtcNow;
    }

    public Payment Pay(PaymentMethod method, decimal amount, string? reference)
    {
        var payment = new Payment(TenantId, Id, method, amount, Currency, reference);
        payment.Capture();
        _payments.Add(payment);

        if (AmountDue <= 0)
        {
            Status = OrderStatus.Paid;
            ClosedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new OrderPaidEvent(Id, TenantId, LocationId));
        }
        else
        {
            Status = OrderStatus.PartiallyPaid;
        }
        return payment;
    }

    public Refund IssueRefund(decimal amount, string reason, Guid issuedByEmployeeId)
    {
        if (amount <= 0 || amount > AmountPaid) throw new DomainException("Invalid refund amount.");
        var refund = new Refund(TenantId, Id, amount, reason, issuedByEmployeeId);
        foreach (var payment in _payments.Where(p => p.Status == PaymentStatus.Captured))
        {
            payment.MarkRefunded();
        }
        return refund;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Paid) throw new InvalidOrderStateException("Cannot cancel a paid order; issue a refund instead.");
        Status = OrderStatus.Cancelled;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureMutable()
    {
        if (Status is OrderStatus.Paid or OrderStatus.Cancelled)
            throw new InvalidOrderStateException($"Order in status {Status} cannot be modified.");
    }
}

public class OrderItem : TenantAuditableEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string VariantName { get; private set; } = default!;
    public Money UnitPrice { get; private set; } = default!;
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<OrderItemModifier> _modifiers = [];
    public IReadOnlyCollection<OrderItemModifier> Modifiers => _modifiers.AsReadOnly();

    public decimal LineTotal => (UnitPrice.Amount + _modifiers.Sum(m => m.PriceDelta)) * Quantity;

    private OrderItem() { }

    internal OrderItem(Guid tenantId, Guid orderId, Guid productVariantId, string productName, string variantName,
        Money unitPrice, int quantity, string? notes)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        TenantId = tenantId;
        OrderId = orderId;
        ProductVariantId = productVariantId;
        ProductName = productName;
        VariantName = variantName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Notes = notes;
    }

    public void AddModifier(string name, decimal priceDelta) => _modifiers.Add(new OrderItemModifier(Id, name, priceDelta));

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        Quantity = quantity;
    }
}

public class OrderItemModifier : BaseEntity
{
    public Guid OrderItemId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal PriceDelta { get; private set; }

    private OrderItemModifier() { }

    internal OrderItemModifier(Guid orderItemId, string name, decimal priceDelta)
    {
        OrderItemId = orderItemId;
        Name = name;
        PriceDelta = priceDelta;
    }
}

public class DiscountApplication : TenantAuditableEntity
{
    public Guid OrderId { get; private set; }
    public DiscountType Type { get; private set; }
    public decimal AmountOff { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid AppliedByEmployeeId { get; private set; }

    private DiscountApplication() { }

    internal DiscountApplication(Guid tenantId, Guid orderId, DiscountType type, decimal amountOff, string reason, Guid appliedByEmployeeId)
    {
        TenantId = tenantId;
        OrderId = orderId;
        Type = type;
        AmountOff = amountOff;
        Reason = reason;
        AppliedByEmployeeId = appliedByEmployeeId;
    }
}

public class Payment : TenantAuditableEntity
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? Reference { get; private set; } // card auth code, voucher code, room number...
    public DateTimeOffset? CapturedAt { get; private set; }

    private Payment() { }

    internal Payment(Guid tenantId, Guid orderId, PaymentMethod method, decimal amount, string currency, string? reference)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        TenantId = tenantId;
        OrderId = orderId;
        Method = method;
        Amount = amount;
        Currency = currency;
        Reference = reference;
    }

    public void Capture()
    {
        Status = PaymentStatus.Captured;
        CapturedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed() => Status = PaymentStatus.Failed;
    public void MarkRefunded() => Status = PaymentStatus.Refunded;
}

public class Refund : TenantAuditableEntity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid IssuedByEmployeeId { get; private set; }

    private Refund() { }

    internal Refund(Guid tenantId, Guid orderId, decimal amount, string reason, Guid issuedByEmployeeId)
    {
        TenantId = tenantId;
        OrderId = orderId;
        Amount = amount;
        Reason = reason;
        IssuedByEmployeeId = issuedByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
