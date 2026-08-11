using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Dashboard;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/dashboard")]
[Authorize(Policy = Permissions.Dashboard.View)]
public sealed class DashboardController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("locations/{locationId:guid}/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(Guid locationId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetDashboardSummaryQuery(TenantId, locationId), ct));
}
