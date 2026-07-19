using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Domain.Menu;

public class MenuCategory : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private MenuCategory() { }

    public MenuCategory(Guid tenantId, Guid locationId, string name, int sortOrder)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Name = name;
        SortOrder = sortOrder;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Product AddProduct(string name, string description, Money basePrice)
    {
        var product = new Product(TenantId, Id, name, description, basePrice);
        _products.Add(product);
        return product;
    }

    public void Deactivate() => IsActive = false;
}

public class Product : TenantAuditableEntity
{
    public Guid CategoryId { get; private set; }
    public Guid? RecipeId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Money BasePrice { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsHappyHour { get; private set; }
    public bool IsSeasonal { get; private set; }
    public int? CaloriesPerServing { get; private set; }

    private readonly List<ProductVariant> _variants = [];
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    private readonly List<ProductModifierGroup> _modifierGroups = [];
    public IReadOnlyCollection<ProductModifierGroup> ModifierGroups => _modifierGroups.AsReadOnly();

    private readonly List<Allergen> _allergens = [];
    public IReadOnlyCollection<Allergen> Allergens => _allergens.AsReadOnly();

    private readonly List<MenuAvailabilityRule> _availabilityRules = [];
    public IReadOnlyCollection<MenuAvailabilityRule> AvailabilityRules => _availabilityRules.AsReadOnly();

    private Product() { }

    internal Product(Guid tenantId, Guid categoryId, string name, string description, Money basePrice)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        CreatedAt = DateTimeOffset.UtcNow;

        // Every product has an implicit "Regular" default variant so POS can always sell by variant.
        _variants.Add(new ProductVariant(tenantId, Id, "Regular", 0m, isDefault: true));
    }

    public ProductVariant AddVariant(string name, decimal priceDelta)
    {
        var variant = new ProductVariant(TenantId, Id, name, priceDelta, isDefault: false);
        _variants.Add(variant);
        return variant;
    }

    public ProductModifierGroup AddModifierGroup(string name, bool isRequired, int maxSelections)
    {
        var group = new ProductModifierGroup(TenantId, Id, name, isRequired, maxSelections);
        _modifierGroups.Add(group);
        return group;
    }

    public void SetAllergens(IEnumerable<Allergen> allergens)
    {
        _allergens.Clear();
        _allergens.AddRange(allergens.Distinct());
    }

    public void SetAvailabilityRule(MenuAvailabilityRule rule) => _availabilityRules.Add(rule);

    public void LinkRecipe(Guid recipeId) => RecipeId = recipeId;
    public void MarkHappyHour(bool value) => IsHappyHour = value;
    public void MarkSeasonal(bool value) => IsSeasonal = value;
    public void UpdatePrice(Money price) => BasePrice = price;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetImage(string url) => ImageUrl = url;
}

public class ProductVariant : TenantAuditableEntity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal PriceDelta { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductVariant() { }

    internal ProductVariant(Guid tenantId, Guid productId, string name, decimal priceDelta, bool isDefault)
    {
        TenantId = tenantId;
        ProductId = productId;
        Name = name;
        PriceDelta = priceDelta;
        IsDefault = isDefault;
    }
}

public class ProductModifierGroup : TenantAuditableEntity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!; // e.g. "Toppings", "Sauce", "Size add-ons"
    public bool IsRequired { get; private set; }
    public int MaxSelections { get; private set; }

    private readonly List<Modifier> _modifiers = [];
    public IReadOnlyCollection<Modifier> Modifiers => _modifiers.AsReadOnly();

    private ProductModifierGroup() { }

    internal ProductModifierGroup(Guid tenantId, Guid productId, string name, bool isRequired, int maxSelections)
    {
        TenantId = tenantId;
        ProductId = productId;
        Name = name;
        IsRequired = isRequired;
        MaxSelections = maxSelections;
    }

    public Modifier AddModifier(string name, decimal priceDelta)
    {
        var modifier = new Modifier(TenantId, Id, name, priceDelta);
        _modifiers.Add(modifier);
        return modifier;
    }
}

public class Modifier : TenantAuditableEntity
{
    public Guid ModifierGroupId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal PriceDelta { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Modifier() { }

    internal Modifier(Guid tenantId, Guid modifierGroupId, string name, decimal priceDelta)
    {
        TenantId = tenantId;
        ModifierGroupId = modifierGroupId;
        Name = name;
        PriceDelta = priceDelta;
    }
}

/// <summary>Restricts a product to certain days/hours — powers seasonal menus and happy hour windows.</summary>
public class MenuAvailabilityRule : BaseEntity
{
    public Guid ProductId { get; private set; }
    public DayOfWeek? DayOfWeek { get; private set; }
    public TimeRange? Hours { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public MenuAvailabilityRule(Guid productId, DayOfWeek? dayOfWeek, TimeRange? hours, DateOnly? startDate, DateOnly? endDate)
    {
        ProductId = productId;
        DayOfWeek = dayOfWeek;
        Hours = hours;
        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsAvailable(DateTimeOffset at)
    {
        if (DayOfWeek is not null && at.DayOfWeek != DayOfWeek) return false;
        if (Hours is not null && !Hours.Contains(TimeOnly.FromDateTime(at.DateTime))) return false;
        var date = DateOnly.FromDateTime(at.DateTime);
        if (StartDate is not null && date < StartDate) return false;
        if (EndDate is not null && date > EndDate) return false;
        return true;
    }
}
