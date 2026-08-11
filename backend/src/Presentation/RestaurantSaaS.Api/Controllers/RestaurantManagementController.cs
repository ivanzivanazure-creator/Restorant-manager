using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.RestaurantManagement;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/restaurant-management")]
public sealed class RestaurantManagementController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("restaurants")]
    public async Task<ActionResult<IReadOnlyCollection<RestaurantDto>>> GetRestaurants(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetRestaurantsQuery(TenantId), ct));

    [HttpPost("restaurants")]
    [Authorize(Policy = Permissions.Tenancy.ManageRestaurants)]
    public async Task<ActionResult<RestaurantDto>> CreateRestaurant(CreateRestaurantRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateRestaurantCommand(TenantId, body.Name, body.LegalName, body.DefaultCurrency), ct));

    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyCollection<LocationDto>>> GetLocations([FromQuery] Guid? restaurantId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetLocationsQuery(TenantId, restaurantId), ct));

    [HttpPost("restaurants/{restaurantId:guid}/locations")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<ActionResult<LocationDto>> CreateLocation(Guid restaurantId, CreateLocationRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateLocationCommand(TenantId, restaurantId, body.Name, body.AddressLine1, body.City, body.Country, body.Currency), ct));

    [HttpPut("locations/{locationId:guid}/working-hours")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<IActionResult> UpdateWorkingHours(Guid locationId, IReadOnlyCollection<WorkingHourInput> hours, CancellationToken ct)
    {
        await Mediator.Send(new UpdateWorkingHoursCommand(TenantId, locationId, hours), ct);
        return NoContent();
    }

    [HttpPut("locations/{locationId:guid}/tax-config")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<IActionResult> UpdateTaxConfig(Guid locationId, UpdateTaxConfigRequest body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateTaxConfigCommand(TenantId, locationId, body.TaxRatePercent, body.TaxLabel, body.PricesIncludeTax), ct);
        return NoContent();
    }

    [HttpPost("locations/{locationId:guid}/tables")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<ActionResult<TableDto>> CreateTable(Guid locationId, CreateTableRequest body, CancellationToken ct) =>
        Ok(await Mediator.Send(new CreateTableCommand(TenantId, locationId, body.Label, body.Capacity, body.Shape, body.X, body.Y), ct));

    [HttpGet("locations/{locationId:guid}/tables")]
    [Authorize(Policy = Permissions.Pos.ViewOrders)]
    public async Task<ActionResult<IReadOnlyCollection<TableDto>>> GetTables(Guid locationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetLocationTablesQuery(TenantId, locationId), ct));

    [HttpPut("locations/{locationId:guid}/tables/layout")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<IActionResult> UpdateLayout(Guid locationId, IReadOnlyCollection<TablePosition> positions, CancellationToken ct)
    {
        await Mediator.Send(new UpdateTableLayoutCommand(TenantId, locationId, positions), ct);
        return NoContent();
    }

    [HttpPost("tables/{tableId:guid}/qr-code")]
    [Authorize(Policy = Permissions.Tenancy.ManageLocations)]
    public async Task<ActionResult<TableDto>> GenerateQrCode(Guid tableId, [FromQuery] string selfOrderBaseUrl, CancellationToken ct) =>
        Ok(await Mediator.Send(new GenerateTableQrCodeCommand(TenantId, tableId, selfOrderBaseUrl), ct));
}

public sealed record CreateRestaurantRequest(string Name, string LegalName, string DefaultCurrency);
public sealed record CreateLocationRequest(string Name, string AddressLine1, string City, string Country, string Currency);
public sealed record UpdateTaxConfigRequest(decimal TaxRatePercent, string TaxLabel, bool PricesIncludeTax);
public sealed record CreateTableRequest(string Label, int Capacity, Domain.Enums.TableShape Shape, float X, float Y);
