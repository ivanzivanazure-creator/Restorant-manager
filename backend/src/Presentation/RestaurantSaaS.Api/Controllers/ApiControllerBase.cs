using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Infrastructure.Identity;

namespace RestaurantSaaS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase(ISender mediator) : ControllerBase
{
    protected ISender Mediator { get; } = mediator;

    /// <summary>The authenticated caller's tenant (RestaurantOwner id). Throws if called by a SuperAdmin
    /// principal or an unauthenticated context — use only from endpoints that require a tenant.</summary>
    protected Guid TenantId => Guid.Parse(User.FindFirstValue(ClaimTypesExt.TenantId)!);

    protected Guid? TenantIdOrNull => Guid.TryParse(User.FindFirstValue(ClaimTypesExt.TenantId), out var id) ? id : null;

    protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
