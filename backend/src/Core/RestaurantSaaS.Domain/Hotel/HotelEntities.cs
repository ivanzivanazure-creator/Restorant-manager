using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Hotel;

public class Room : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public string Number { get; private set; } = default!;
    public RoomType Type { get; private set; }
    public decimal NightlyRate { get; private set; }
    public RoomStatus Status { get; private set; } = RoomStatus.Vacant;
    public int Floor { get; private set; }

    private Room() { }

    public Room(Guid tenantId, Guid locationId, string number, RoomType type, decimal nightlyRate, int floor)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Number = number;
        Type = type;
        NightlyRate = nightlyRate;
        Floor = floor;
    }

    public void MarkOccupied() => Status = RoomStatus.Occupied;
    public void MarkVacant() => Status = RoomStatus.Vacant;
    public void MarkCleaning() => Status = RoomStatus.Cleaning;
    public void MarkOutOfService() => Status = RoomStatus.OutOfService;
}

public class Guest : TenantAuditableEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? PassportOrIdNumber { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Guest() { }

    public Guest(Guid tenantId, string firstName, string lastName, string email, string? phone, string? passportOrIdNumber)
    {
        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        PassportOrIdNumber = passportOrIdNumber;
    }
}

public class Reservation : TenantAuditableEntity
{
    public Guid RoomId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Requested;
    public int GuestCount { get; private set; }

    private Reservation() { }

    public Reservation(Guid tenantId, Guid roomId, Guid guestId, DateOnly checkInDate, DateOnly checkOutDate, int guestCount)
    {
        if (checkOutDate <= checkInDate) throw new DomainException("Check-out date must be after check-in date.");
        TenantId = tenantId;
        RoomId = roomId;
        GuestId = guestId;
        CheckInDate = checkInDate;
        CheckOutDate = checkOutDate;
        GuestCount = guestCount;
    }

    public void Confirm() => Status = ReservationStatus.Confirmed;
    public void Cancel() => Status = ReservationStatus.Cancelled;
    public void MarkNoShow() => Status = ReservationStatus.NoShow;

    public GuestStay CheckIn()
    {
        if (Status != ReservationStatus.Confirmed) throw new DomainException("Only confirmed reservations can check in.");
        Status = ReservationStatus.CheckedIn;
        return new GuestStay(TenantId, Id, RoomId, GuestId);
    }
}

public class GuestStay : TenantAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateTimeOffset CheckInAt { get; private set; }
    public DateTimeOffset? CheckOutAt { get; private set; }
    public decimal RoomChargeBalance { get; private set; }

    private readonly List<RoomServiceOrder> _roomServiceOrders = [];
    public IReadOnlyCollection<RoomServiceOrder> RoomServiceOrders => _roomServiceOrders.AsReadOnly();

    private readonly List<MinibarCharge> _minibarCharges = [];
    public IReadOnlyCollection<MinibarCharge> MinibarCharges => _minibarCharges.AsReadOnly();

    internal GuestStay(Guid tenantId, Guid reservationId, Guid roomId, Guid guestId)
    {
        TenantId = tenantId;
        ReservationId = reservationId;
        RoomId = roomId;
        GuestId = guestId;
        CheckInAt = DateTimeOffset.UtcNow;
    }

    private GuestStay() { }

    public void ChargeToRoom(decimal amount) => RoomChargeBalance += amount;

    public RoomServiceOrder RequestRoomService(Guid orderId, decimal amount)
    {
        var request = new RoomServiceOrder(Id, orderId, amount);
        _roomServiceOrders.Add(request);
        ChargeToRoom(amount);
        return request;
    }

    public MinibarCharge AddMinibarCharge(string itemName, decimal amount)
    {
        var charge = new MinibarCharge(Id, itemName, amount);
        _minibarCharges.Add(charge);
        ChargeToRoom(amount);
        return charge;
    }

    public void CheckOut()
    {
        if (RoomChargeBalance > 0) throw new DomainException("Cannot check out with an outstanding room-charge balance; settle first.");
        CheckOutAt = DateTimeOffset.UtcNow;
    }

    public void SettleBalance(decimal amount) => RoomChargeBalance = Math.Max(0, RoomChargeBalance - amount);
}

public class RoomServiceOrder : BaseEntity
{
    public Guid GuestStayId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }

    private RoomServiceOrder() { }

    internal RoomServiceOrder(Guid guestStayId, Guid orderId, decimal amount)
    {
        GuestStayId = guestStayId;
        OrderId = orderId;
        Amount = amount;
        RequestedAt = DateTimeOffset.UtcNow;
    }
}

public class MinibarCharge : BaseEntity
{
    public Guid GuestStayId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public DateTimeOffset ChargedAt { get; private set; }

    private MinibarCharge() { }

    internal MinibarCharge(Guid guestStayId, string itemName, decimal amount)
    {
        GuestStayId = guestStayId;
        ItemName = itemName;
        Amount = amount;
        ChargedAt = DateTimeOffset.UtcNow;
    }
}
