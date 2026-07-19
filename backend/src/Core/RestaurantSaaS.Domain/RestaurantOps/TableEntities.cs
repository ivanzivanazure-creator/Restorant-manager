using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Domain.RestaurantOps;

public class Table : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public string Label { get; private set; } = default!;
    public int Capacity { get; private set; }
    public TableShape Shape { get; private set; } = TableShape.Round;
    public TableStatus Status { get; private set; } = TableStatus.Free;
    public float LayoutX { get; private set; }
    public float LayoutY { get; private set; }
    public Guid? LinkedRoomId { get; private set; } // hotel: table permanently tied to a room (e.g. room service)

    public QrCode? QrCode { get; private set; }

    private Table() { }

    public Table(Guid tenantId, Guid locationId, string label, int capacity, TableShape shape, float x, float y)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Label = label;
        Capacity = capacity;
        Shape = shape;
        LayoutX = x;
        LayoutY = y;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MoveTo(float x, float y)
    {
        LayoutX = x;
        LayoutY = y;
    }

    public void Occupy() => Status = TableStatus.Occupied;
    public void Free() => Status = TableStatus.Free;
    public void Reserve() => Status = TableStatus.Reserved;
    public void SetCleaning() => Status = TableStatus.Cleaning;

    public void LinkToRoom(Guid roomId) => LinkedRoomId = roomId;

    public void AttachQrCode(QrCode qrCode) => QrCode = qrCode;
}

/// <summary>A QR code that deep-links to the self-order page for a given table, scoped to the tenant.</summary>
public class QrCode : BaseEntity
{
    public Guid TableId { get; private set; }
    public string Token { get; private set; } = default!;
    public string ImageUrl { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private QrCode() { }

    public QrCode(Guid tableId, string token, string imageUrl)
    {
        TableId = tableId;
        Token = token;
        ImageUrl = imageUrl;
    }

    public void Revoke() => IsActive = false;
}
