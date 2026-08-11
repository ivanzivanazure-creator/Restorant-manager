using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Menu;

public sealed record ModifierDto(Guid Id, string Name, decimal PriceDelta, bool IsActive);
public sealed record ModifierGroupDto(Guid Id, string Name, bool IsRequired, int MaxSelections, IReadOnlyCollection<ModifierDto> Modifiers);
public sealed record VariantDto(Guid Id, string Name, decimal PriceDelta, bool IsDefault, bool IsActive);

public sealed record ProductDto(
    Guid Id, string Name, string Description, decimal BasePrice, string Currency, string? ImageUrl,
    bool IsActive, bool IsHappyHour, bool IsSeasonal, IReadOnlyCollection<Allergen> Allergens,
    IReadOnlyCollection<VariantDto> Variants, IReadOnlyCollection<ModifierGroupDto> ModifierGroups);

public sealed record MenuCategoryDto(Guid Id, string Name, int SortOrder, IReadOnlyCollection<ProductDto> Products);

public sealed record GetMenuQuery(Guid TenantId, Guid LocationId, bool ActiveOnly = true) : IRequest<IReadOnlyCollection<MenuCategoryDto>>, ITenantScopedRequest;

public sealed class GetMenuQueryHandler(IApplicationDbContext db) : IRequestHandler<GetMenuQuery, IReadOnlyCollection<MenuCategoryDto>>
{
    public async Task<IReadOnlyCollection<MenuCategoryDto>> Handle(GetMenuQuery request, CancellationToken ct)
    {
        var categories = await db.MenuCategories
            .Where(c => c.LocationId == request.LocationId && c.TenantId == request.TenantId && (!request.ActiveOnly || c.IsActive))
            .Include(c => c.Products).ThenInclude(p => p.Variants)
            .Include(c => c.Products).ThenInclude(p => p.ModifierGroups).ThenInclude(g => g.Modifiers)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return categories.Select(c => new MenuCategoryDto(
            c.Id, c.Name, c.SortOrder,
            c.Products.Where(p => !request.ActiveOnly || p.IsActive).Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.BasePrice.Amount, p.BasePrice.Currency, p.ImageUrl,
                p.IsActive, p.IsHappyHour, p.IsSeasonal, p.Allergens,
                p.Variants.Where(v => !request.ActiveOnly || v.IsActive)
                    .Select(v => new VariantDto(v.Id, v.Name, v.PriceDelta, v.IsDefault, v.IsActive)).ToList(),
                p.ModifierGroups.Select(g => new ModifierGroupDto(g.Id, g.Name, g.IsRequired, g.MaxSelections,
                    g.Modifiers.Where(m => !request.ActiveOnly || m.IsActive)
                        .Select(m => new ModifierDto(m.Id, m.Name, m.PriceDelta, m.IsActive)).ToList())).ToList()
            )).ToList())).ToList();
    }
}
