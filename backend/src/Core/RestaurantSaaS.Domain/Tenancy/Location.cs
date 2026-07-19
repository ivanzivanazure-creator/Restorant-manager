using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Domain.Tenancy;

public class Location : TenantAuditableEntity
{
    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; } = default!;
    public Address Address { get; private set; } = default!;
    public GeoCoordinates? Coordinates { get; private set; }
    public string Currency { get; private set; } = default!;
    public string TimeZoneId { get; private set; } = "UTC";
    public IReadOnlyCollection<string> SupportedLanguages { get; private set; } = ["en"];
    public bool IsActive { get; private set; } = true;

    private readonly List<WorkingHour> _workingHours = [];
    public IReadOnlyCollection<WorkingHour> WorkingHours => _workingHours.AsReadOnly();

    public TaxConfig TaxConfig { get; private set; } = default!;

    private Location() { }

    internal Location(Guid tenantId, Guid restaurantId, string name, string addressLine1, string city, string country, string currency)
    {
        TenantId = tenantId;
        RestaurantId = restaurantId;
        Name = name;
        Address = new Address(addressLine1, null, city, null, string.Empty, country);
        Currency = currency;
        TaxConfig = new TaxConfig(TenantId, Id, 0m, "VAT");
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void SetWorkingHours(IEnumerable<WorkingHour> hours)
    {
        _workingHours.Clear();
        _workingHours.AddRange(hours);
    }

    public void SetTaxConfig(TaxConfig taxConfig) => TaxConfig = taxConfig;

    public void Deactivate() => IsActive = false;
}

public class WorkingHour : BaseEntity
{
    public Guid LocationId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeRange Hours { get; private set; } = default!;
    public bool IsClosed { get; private set; }

    private WorkingHour() { }

    public WorkingHour(Guid locationId, DayOfWeek dayOfWeek, TimeRange hours, bool isClosed = false)
    {
        LocationId = locationId;
        DayOfWeek = dayOfWeek;
        Hours = hours;
        IsClosed = isClosed;
    }
}

public class TaxConfig : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public decimal DefaultTaxRatePercent { get; private set; }
    public string TaxLabel { get; private set; } = default!;
    public bool PricesIncludeTax { get; private set; }

    private TaxConfig() { }

    public TaxConfig(Guid tenantId, Guid locationId, decimal defaultTaxRatePercent, string taxLabel, bool pricesIncludeTax = true)
    {
        TenantId = tenantId;
        LocationId = locationId;
        DefaultTaxRatePercent = defaultTaxRatePercent;
        TaxLabel = taxLabel;
        PricesIncludeTax = pricesIncludeTax;
    }

    public void Update(decimal rate, string label, bool pricesIncludeTax)
    {
        DefaultTaxRatePercent = rate;
        TaxLabel = label;
        PricesIncludeTax = pricesIncludeTax;
    }
}
