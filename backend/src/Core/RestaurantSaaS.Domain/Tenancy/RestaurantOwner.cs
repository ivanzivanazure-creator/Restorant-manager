using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Domain.Tenancy;

/// <summary>The billable tenant. One RestaurantOwner can own several Restaurant brands.</summary>
public class RestaurantOwner : AuditableEntity
{
    public string CompanyName { get; private set; } = default!;
    public string ContactEmail { get; private set; } = default!;
    public string? ContactPhone { get; private set; }
    public TenantStatus Status { get; private set; } = TenantStatus.Trial;
    public Guid PrimaryUserId { get; private set; }

    private readonly List<Restaurant> _restaurants = [];
    public IReadOnlyCollection<Restaurant> Restaurants => _restaurants.AsReadOnly();

    private RestaurantOwner() { }

    public RestaurantOwner(string companyName, string contactEmail, Guid primaryUserId)
    {
        CompanyName = companyName;
        ContactEmail = contactEmail;
        PrimaryUserId = primaryUserId;
        Status = TenantStatus.Trial;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate() => Status = TenantStatus.Active;
    public void Suspend() => Status = TenantStatus.Suspended;
    public void Cancel() => Status = TenantStatus.Cancelled;

    public Restaurant AddRestaurant(string name, string legalName, string defaultCurrency)
    {
        var restaurant = new Restaurant(Id, name, legalName, defaultCurrency);
        _restaurants.Add(restaurant);
        return restaurant;
    }
}

public class Restaurant : TenantAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string LegalName { get; private set; } = default!;
    public string DefaultCurrency { get; private set; } = default!;
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Location> _locations = [];
    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

    private Restaurant() { }

    internal Restaurant(Guid tenantId, string name, string legalName, string defaultCurrency)
    {
        TenantId = tenantId;
        Name = name;
        LegalName = legalName;
        DefaultCurrency = defaultCurrency;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Location AddLocation(string name, string addressLine1, string city, string country, string currency)
    {
        var location = new Location(TenantId, Id, name, addressLine1, city, country, currency);
        _locations.Add(location);
        return location;
    }

    public void Deactivate() => IsActive = false;
}
