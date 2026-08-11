namespace RestaurantSaaS.Domain.Enums;

public enum TenantStatus { Trial, Active, Suspended, Cancelled }

public enum BillingCycle { Monthly, Yearly }

public enum SubscriptionStatus { Trialing, Active, PastDue, Locked, Cancelled }

public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Void }

public enum TableShape { Round, Square, Rectangle }

public enum TableStatus { Free, Occupied, Reserved, Cleaning, OutOfService }

public enum OrderStatus { Open, InKitchen, ReadyToServe, Served, PartiallyPaid, Paid, Cancelled }

public enum OrderSource { Pos, QrSelfOrder, RoomService, ThirdPartyDelivery }

public enum PaymentMethod { Cash, Card, Voucher, RoomCharge, GiftCard, MobileWallet }

public enum PaymentStatus { Pending, Captured, PartiallyRefunded, Refunded, Failed }

public enum DiscountType { Percentage, FixedAmount, HappyHour, Coupon }

public enum KitchenTicketStatus { Queued, InProgress, Ready, Served, Cancelled }

public enum KitchenTicketPriority { Normal, Rush, Vip }

public enum StockMovementType { Receipt, Sale, Transfer, Correction, Waste, ProductionConsumption }

public enum PurchaseOrderStatus { Draft, PendingApproval, Approved, Ordered, PartiallyReceived, Received, Cancelled }

public enum RoomStatus { Vacant, Occupied, Cleaning, OutOfService }

public enum RoomType { Standard, Deluxe, Suite, Executive }

public enum ReservationStatus { Requested, Confirmed, CheckedIn, CheckedOut, Cancelled, NoShow }

public enum EmploymentStatus { Active, OnLeave, Terminated }

public enum ShiftStatus { Scheduled, InProgress, Completed, Missed }

public enum LeaveType { Vacation, Sick, Unpaid, Other }

public enum LeaveRequestStatus { Pending, Approved, Rejected, Cancelled }

public enum NotificationChannel { Email, Sms, Push, InApp }

public enum NotificationCategory { LowInventory, OrderReady, ReservationReminder, SubscriptionExpiring, System }

public enum Allergen
{
    Gluten, Crustaceans, Eggs, Fish, Peanuts, Soybeans, Milk, Nuts,
    Celery, Mustard, Sesame, Sulphites, Lupin, Molluscs
}

public enum MeasurementUnit { Gram, Kilogram, Milliliter, Liter, Piece, Portion }

public enum DeliveryPlatform { UberEats, DoorDash, GrubHub, Deliveroo, Other }

public enum SlaTier { Standard, Premium }

public enum IncidentStatus { Investigating, Identified, Monitoring, Resolved }

public enum IncidentSeverity { Minor, Major, Critical }

public enum PlatformComponent { Api, Database, Cache, Realtime, BackgroundJobs }

public enum ComponentHealth { Operational, DegradedPerformance, PartialOutage, MajorOutage }
