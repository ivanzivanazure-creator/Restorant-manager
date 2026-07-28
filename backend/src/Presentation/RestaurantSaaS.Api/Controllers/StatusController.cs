using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Application.Status;

namespace RestaurantSaaS.Api.Controllers;

/// <summary>Public status page + SuperAdmin incident management. GETs are unauthenticated by design —
/// the whole point of a status page is that customers (and prospects) can check it without logging in.</summary>
[Route("api/v1/status")]
[AllowAnonymous]
public sealed class StatusController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    public async Task<ActionResult<PublicStatusDto>> GetStatus(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPublicStatusQuery(), ct));

    [HttpGet("incidents")]
    public async Task<ActionResult<IReadOnlyCollection<IncidentDto>>> ListIncidents([FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new ListIncidentsQuery(take), ct));

    [HttpPost("incidents")]
    [Authorize(Policy = Permissions.Status.ManageIncidents)]
    public async Task<ActionResult<Guid>> CreateIncident(CreateIncidentCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("incidents/{incidentId:guid}/updates")]
    [Authorize(Policy = Permissions.Status.ManageIncidents)]
    public async Task<IActionResult> PostUpdate(Guid incidentId, PostIncidentUpdateRequest body, CancellationToken ct)
    {
        await Mediator.Send(new PostIncidentUpdateCommand(incidentId, body.Status, body.Message), ct);
        return NoContent();
    }
}

public sealed record PostIncidentUpdateRequest(Domain.Enums.IncidentStatus Status, string Message);
