using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Recipes;

public class Recipe : TenantAuditableEntity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = default!;
    public int PrepMinutes { get; private set; }
    public int CookMinutes { get; private set; }
    public int CurrentVersionNumber { get; private set; } = 1;
    public string? PrimaryPhotoUrl { get; private set; }
    public string? VideoUrl { get; private set; }

    private readonly List<RecipeIngredient> _ingredients = [];
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    private readonly List<RecipeStep> _steps = [];
    public IReadOnlyCollection<RecipeStep> Steps => _steps.AsReadOnly();

    private readonly List<RecipeVersion> _versions = [];
    public IReadOnlyCollection<RecipeVersion> Versions => _versions.AsReadOnly();

    public int TotalMinutes => PrepMinutes + CookMinutes;

    private Recipe() { }

    public Recipe(Guid tenantId, Guid productId, string name, int prepMinutes, int cookMinutes)
    {
        TenantId = tenantId;
        ProductId = productId;
        Name = name;
        PrepMinutes = prepMinutes;
        CookMinutes = cookMinutes;
    }

    public RecipeIngredient AddIngredient(Guid ingredientId, decimal quantity, string unit)
    {
        var line = new RecipeIngredient(Id, ingredientId, quantity, unit);
        _ingredients.Add(line);
        return line;
    }

    public RecipeStep AddStep(int order, string instruction, string? photoUrl = null)
    {
        var step = new RecipeStep(Id, order, instruction, photoUrl);
        _steps.Add(step);
        return step;
    }

    /// <summary>Cost per serving = sum(ingredient qty * current ingredient cost); computed by the Application layer
    /// via a query joining live Ingredient.CostPerUnit, since Domain cannot depend on Inventory's repository.</summary>
    public decimal CalculateCostPerServing(IReadOnlyDictionary<Guid, decimal> ingredientUnitCosts)
    {
        return _ingredients.Sum(i => i.Quantity * ingredientUnitCosts.GetValueOrDefault(i.IngredientId, 0m));
    }

    public RecipeVersion SnapshotVersion(string changeSummary)
    {
        CurrentVersionNumber++;
        var version = new RecipeVersion(Id, CurrentVersionNumber, changeSummary,
            _ingredients.Select(i => (i.IngredientId, i.Quantity, i.Unit)).ToList(),
            _steps.Select(s => s.Instruction).ToList());
        _versions.Add(version);
        return version;
    }

    public void SetMedia(string? photoUrl, string? videoUrl)
    {
        PrimaryPhotoUrl = photoUrl;
        VideoUrl = videoUrl;
    }
}

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = default!;

    private RecipeIngredient() { }

    internal RecipeIngredient(Guid recipeId, Guid ingredientId, decimal quantity, string unit)
    {
        RecipeId = recipeId;
        IngredientId = ingredientId;
        Quantity = quantity;
        Unit = unit;
    }
}

public class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public int Order { get; private set; }
    public string Instruction { get; private set; } = default!;
    public string? PhotoUrl { get; private set; }

    private RecipeStep() { }

    internal RecipeStep(Guid recipeId, int order, string instruction, string? photoUrl)
    {
        RecipeId = recipeId;
        Order = order;
        Instruction = instruction;
        PhotoUrl = photoUrl;
    }
}

public class RecipeVersion : BaseEntity
{
    public Guid RecipeId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ChangeSummary { get; private set; } = default!;
    public DateTimeOffset SnapshotAt { get; private set; }
    public IReadOnlyCollection<(Guid IngredientId, decimal Quantity, string Unit)> IngredientsSnapshot { get; private set; } = [];
    public IReadOnlyCollection<string> StepsSnapshot { get; private set; } = [];

    private RecipeVersion() { }

    internal RecipeVersion(Guid recipeId, int versionNumber, string changeSummary,
        IReadOnlyCollection<(Guid, decimal, string)> ingredientsSnapshot, IReadOnlyCollection<string> stepsSnapshot)
    {
        RecipeId = recipeId;
        VersionNumber = versionNumber;
        ChangeSummary = changeSummary;
        SnapshotAt = DateTimeOffset.UtcNow;
        IngredientsSnapshot = ingredientsSnapshot;
        StepsSnapshot = stepsSnapshot;
    }
}
