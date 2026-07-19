using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Crm;

public class Customer : TenantAuditableEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Customer() { }

    public Customer(Guid tenantId, string firstName, string lastName, string? email, string? phone, DateOnly? dateOfBirth = null)
    {
        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        DateOfBirth = dateOfBirth;
    }
}

public class LoyaltyAccount : TenantAuditableEntity
{
    public Guid CustomerId { get; private set; }
    public int PointsBalance { get; private set; }
    public string TierName { get; private set; } = "Bronze";

    private readonly List<LoyaltyTransaction> _transactions = [];
    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();

    private LoyaltyAccount() { }

    public LoyaltyAccount(Guid tenantId, Guid customerId)
    {
        TenantId = tenantId;
        CustomerId = customerId;
    }

    public void Earn(int points, string reason)
    {
        PointsBalance += points;
        _transactions.Add(new LoyaltyTransaction(Id, points, reason));
    }

    public void Redeem(int points, string reason)
    {
        if (points > PointsBalance) throw new DomainException("Insufficient loyalty points.");
        PointsBalance -= points;
        _transactions.Add(new LoyaltyTransaction(Id, -points, reason));
    }

    public void SetTier(string tierName) => TierName = tierName;
}

public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; private set; }
    public int Points { get; private set; } // positive earn, negative redeem
    public string Reason { get; private set; } = default!;
    public DateTimeOffset OccurredAt { get; private set; }

    private LoyaltyTransaction() { }

    internal LoyaltyTransaction(Guid loyaltyAccountId, int points, string reason)
    {
        LoyaltyAccountId = loyaltyAccountId;
        Points = points;
        Reason = reason;
        OccurredAt = DateTimeOffset.UtcNow;
    }
}

public class Coupon : TenantAuditableEntity
{
    public string Code { get; private set; } = default!;
    public decimal DiscountPercent { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }

    private Coupon() { }

    public Coupon(Guid tenantId, string code, decimal discountPercent, DateTimeOffset validFrom, DateTimeOffset validTo, int? maxRedemptions)
    {
        TenantId = tenantId;
        Code = code;
        DiscountPercent = discountPercent;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MaxRedemptions = maxRedemptions;
    }

    public bool IsValid(DateTimeOffset at) =>
        at >= ValidFrom && at <= ValidTo && (MaxRedemptions is null || RedemptionCount < MaxRedemptions);

    public void Redeem()
    {
        if (!IsValid(DateTimeOffset.UtcNow)) throw new DomainException("Coupon is not valid.");
        RedemptionCount++;
    }
}

public class CouponRedemption : BaseEntity
{
    public Guid CouponId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTimeOffset RedeemedAt { get; private set; }

    public CouponRedemption(Guid couponId, Guid customerId, Guid orderId)
    {
        CouponId = couponId;
        CustomerId = customerId;
        OrderId = orderId;
        RedeemedAt = DateTimeOffset.UtcNow;
    }
}

public class GiftCard : TenantAuditableEntity
{
    public string Code { get; private set; } = default!;
    public Guid? OwnerCustomerId { get; private set; }
    public decimal InitialBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; private set; } = true;

    private GiftCard() { }

    public GiftCard(Guid tenantId, string code, decimal initialBalance, Guid? ownerCustomerId)
    {
        TenantId = tenantId;
        Code = code;
        InitialBalance = initialBalance;
        CurrentBalance = initialBalance;
        OwnerCustomerId = ownerCustomerId;
    }

    public void Redeem(decimal amount)
    {
        if (!IsActive) throw new DomainException("Gift card is not active.");
        if (amount > CurrentBalance) throw new DomainException("Insufficient gift card balance.");
        CurrentBalance -= amount;
    }

    public void TopUp(decimal amount) => CurrentBalance += amount;
    public void Deactivate() => IsActive = false;
}

public class Feedback : TenantAuditableEntity
{
    public Guid? CustomerId { get; private set; }
    public Guid? OrderId { get; private set; }
    public int Rating { get; private set; } // 1-5
    public string? Comment { get; private set; }

    private Feedback() { }

    public Feedback(Guid tenantId, Guid? customerId, Guid? orderId, int rating, string? comment)
    {
        if (rating is < 1 or > 5) throw new DomainException("Rating must be between 1 and 5.");
        TenantId = tenantId;
        CustomerId = customerId;
        OrderId = orderId;
        Rating = rating;
        Comment = comment;
    }
}
