using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.SuperAdmin;

namespace RestaurantSaaS.Api.Controllers;

[Authorize(Policy = Permissions.SuperAdminAccess)]
[Route("api/v1/super-admin")]
public sealed class SuperAdminController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("tenants")]
    public async Task<ActionResult<Application.Common.Models.PaginatedList<TenantSummaryDto>>> ListTenants(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ListTenantsQuery(pageNumber, pageSize, search), ct));

    [HttpPost("tenants/{tenantId:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid tenantId, ActivateTenantSubscriptionCommand body, CancellationToken ct)
    {
        await Mediator.Send(body with { TenantId = tenantId }, ct);
        return NoContent();
    }

    [HttpPost("tenants/{tenantId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid tenantId, CancellationToken ct)
    {
        await Mediator.Send(new DeactivateTenantCommand(tenantId), ct);
        return NoContent();
    }

    [HttpGet("packages")]
    public async Task<ActionResult<IReadOnlyCollection<PackageDto>>> ListPackages([FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ListPackagesQuery(activeOnly), ct));

    [HttpPost("packages")]
    public async Task<ActionResult<PackageDto>> CreatePackage(CreatePackageCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpGet("analytics")]
    public async Task<ActionResult<PlatformAnalyticsDto>> GetAnalytics(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPlatformAnalyticsQuery(), ct));
}
