using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Menu;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/menu")]
public sealed class MenuController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("locations/{locationId:guid}")]
    [Authorize(Policy = Permissions.Menu.View)]
    public async Task<ActionResult<IReadOnlyCollection<MenuCategoryDto>>> GetMenu(Guid locationId, [FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetMenuQuery(TenantId, locationId, activeOnly), ct));

    [HttpPost("locations/{locationId:guid}/categories")]
    [Authorize(Policy = Permissions.Menu.Manage)]
    public async Task<ActionResult<Guid>> CreateCategory(Guid locationId, CreateCategoryRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateMenuCategoryCommand(TenantId, locationId, body.Name, body.SortOrder), ct));

    [HttpPost("categories/{categoryId:guid}/products")]
    [Authorize(Policy = Permissions.Menu.Manage)]
    public async Task<ActionResult<Guid>> CreateProduct(Guid categoryId, CreateProductRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateProductCommand(TenantId, categoryId, body.Name, body.Description, body.Price, body.Currency, body.Allergens), ct));

    [HttpPost("products/{productId:guid}/variants")]
    [Authorize(Policy = Permissions.Menu.Manage)]
    public async Task<ActionResult<Guid>> AddVariant(Guid productId, AddVariantRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AddProductVariantCommand(TenantId, productId, body.Name, body.PriceDelta), ct));

    [HttpPost("products/{productId:guid}/modifier-groups")]
    [Authorize(Policy = Permissions.Menu.Manage)]
    public async Task<ActionResult<Guid>> AddModifierGroup(Guid productId, AddModifierGroupRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AddModifierGroupCommand(TenantId, productId, body.Name, body.IsRequired, body.MaxSelections, body.Modifiers), ct));

    [HttpPut("products/{productId:guid}/active")]
    [Authorize(Policy = Permissions.Menu.Manage)]
    public async Task<IActionResult> SetProductActive(Guid productId, [FromQuery] bool isActive, CancellationToken ct)
    {
        await Mediator.Send(new SetProductActiveCommand(TenantId, productId, isActive), ct);
        return NoContent();
    }
}

public sealed record CreateCategoryRequest(string Name, int SortOrder);
public sealed record CreateProductRequest(string Name, string Description, decimal Price, string Currency, IReadOnlyCollection<Allergen> Allergens);
public sealed record AddVariantRequest(string Name, decimal PriceDelta);
public sealed record AddModifierGroupRequest(string Name, bool IsRequired, int MaxSelections, IReadOnlyCollection<ModifierInput> Modifiers);
