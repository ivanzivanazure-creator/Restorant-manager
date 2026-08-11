using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Onboarding;

namespace RestaurantSaaS.Api.Controllers;

[Route("api/v1/onboarding")]
public sealed class OnboardingController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOnboardingStatusQuery(TenantId), ct));
}
