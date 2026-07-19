using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Kitchen;

/// <summary>The Chef/KDS-facing projection of an order: one ticket per order sent to a kitchen station.</summary>
public class KitchenTicket : TenantAuditableEntity
{
    public Guid OrderId { get; private set; }
    public Guid LocationId { get; private set; }
    public string? TableLabel { get; private set; }
    public KitchenTicketStatus Status { get; private set; } = KitchenTicketStatus.Queued;
    public KitchenTicketPriority Priority { get; private set; } = KitchenTicketPriority.Normal;
    public int TargetCookMinutes { get; private set; }
    public DateTimeOffset QueuedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? ServedAt { get; private set; }

    private readonly List<KitchenTicketItem> _items = [];
    public IReadOnlyCollection<KitchenTicketItem> Items => _items.AsReadOnly();

    public TimeSpan? ElapsedCookTime => StartedAt is null ? null : (ReadyAt ?? DateTimeOffset.UtcNow) - StartedAt;
    public bool IsOverdue => StartedAt is not null && ReadyAt is null
        && DateTimeOffset.UtcNow - StartedAt > TimeSpan.FromMinutes(TargetCookMinutes);

    private KitchenTicket() { }

    public KitchenTicket(Guid tenantId, Guid orderId, Guid locationId, string? tableLabel, int targetCookMinutes)
    {
        TenantId = tenantId;
        OrderId = orderId;
        LocationId = locationId;
        TableLabel = tableLabel;
        TargetCookMinutes = targetCookMinutes;
        QueuedAt = DateTimeOffset.UtcNow;
        CreatedAt = QueuedAt;
    }

    public KitchenTicketItem AddItem(Guid orderItemId, string productName, string variantName, int quantity, string? notes)
    {
        var item = new KitchenTicketItem(Id, orderItemId, productName, variantName, quantity, notes);
        _items.Add(item);
        return item;
    }

    public void SetPriority(KitchenTicketPriority priority) => Priority = priority;

    public void Start()
    {
        if (Status != KitchenTicketStatus.Queued) throw new InvalidOrderStateException("Ticket already started.");
        Status = KitchenTicketStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
        foreach (var item in _items) item.Start();
    }

    public void MarkReady()
    {
        if (Status != KitchenTicketStatus.InProgress) throw new InvalidOrderStateException("Ticket must be in progress to mark ready.");
        Status = KitchenTicketStatus.Ready;
        ReadyAt = DateTimeOffset.UtcNow;
        foreach (var item in _items) item.MarkReady();
    }

    public void MarkServed()
    {
        if (Status != KitchenTicketStatus.Ready) throw new InvalidOrderStateException("Ticket must be ready before serving.");
        Status = KitchenTicketStatus.Served;
        ServedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel() => Status = KitchenTicketStatus.Cancelled;
}

public class KitchenTicketItem : BaseEntity
{
    public Guid KitchenTicketId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string VariantName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }
    public KitchenTicketStatus Status { get; private set; } = KitchenTicketStatus.Queued;

    private KitchenTicketItem() { }

    internal KitchenTicketItem(Guid kitchenTicketId, Guid orderItemId, string productName, string variantName, int quantity, string? notes)
    {
        KitchenTicketId = kitchenTicketId;
        OrderItemId = orderItemId;
        ProductName = productName;
        VariantName = variantName;
        Quantity = quantity;
        Notes = notes;
    }

    public void Start() => Status = KitchenTicketStatus.InProgress;
    public void MarkReady() => Status = KitchenTicketStatus.Ready;
}
