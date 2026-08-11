using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Application.Menu;

public sealed record CreateMenuCategoryCommand(Guid TenantId, Guid LocationId, string Name, int SortOrder)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class CreateMenuCategoryCommandValidator : AbstractValidator<CreateMenuCategoryCommand>
{
    public CreateMenuCategoryCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public sealed class CreateMenuCategoryCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateMenuCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateMenuCategoryCommand request, CancellationToken ct)
    {
        var category = new MenuCategory(request.TenantId, request.LocationId, request.Name, request.SortOrder);
        db.MenuCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }
}

public sealed record CreateProductCommand(
    Guid TenantId, Guid CategoryId, string Name, string Description, decimal Price, string Currency, IReadOnlyCollection<Allergen> Allergens)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class CreateProductCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var category = await db.MenuCategories.SingleOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(MenuCategory), request.CategoryId);

        var product = category.AddProduct(request.Name, request.Description, Money.Of(request.Price, request.Currency));
        product.SetAllergens(request.Allergens);

        await db.SaveChangesAsync(ct);
        return product.Id;
    }
}

public sealed record AddProductVariantCommand(Guid TenantId, Guid ProductId, string Name, decimal PriceDelta) : IRequest<Guid>, ITenantScopedRequest;

public sealed class AddProductVariantCommandHandler(IApplicationDbContext db) : IRequestHandler<AddProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(AddProductVariantCommand request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var variant = product.AddVariant(request.Name, request.PriceDelta);
        await db.SaveChangesAsync(ct);
        return variant.Id;
    }
}

public sealed record ModifierInput(string Name, decimal PriceDelta);

public sealed record AddModifierGroupCommand(
    Guid TenantId, Guid ProductId, string Name, bool IsRequired, int MaxSelections, IReadOnlyCollection<ModifierInput> Modifiers)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class AddModifierGroupCommandHandler(IApplicationDbContext db) : IRequestHandler<AddModifierGroupCommand, Guid>
{
    public async Task<Guid> Handle(AddModifierGroupCommand request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var group = product.AddModifierGroup(request.Name, request.IsRequired, request.MaxSelections);
        foreach (var modifier in request.Modifiers)
        {
            group.AddModifier(modifier.Name, modifier.PriceDelta);
        }

        await db.SaveChangesAsync(ct);
        return group.Id;
    }
}

public sealed record SetProductActiveCommand(Guid TenantId, Guid ProductId, bool IsActive) : IRequest, ITenantScopedRequest;

public sealed class SetProductActiveCommandHandler(IApplicationDbContext db) : IRequestHandler<SetProductActiveCommand>
{
    public async Task Handle(SetProductActiveCommand request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (request.IsActive) product.Activate(); else product.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
