using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Status;

public sealed record ComponentStatusDto(PlatformComponent Component, ComponentHealth Health);

public sealed record PublicStatusDto(
    ComponentHealth OverallHealth, IReadOnlyCollection<ComponentStatusDto> Components, IReadOnlyCollection<IncidentDto> RecentIncidents);

/// <summary>Unauthenticated, intentionally minimal-detail: backs the public status page. Never leaks
/// tenant data, connection strings, or stack traces — only component-level health + incident history.</summary>
public sealed record GetPublicStatusQuery : IRequest<PublicStatusDto>;

public sealed class GetPublicStatusQueryHandler(IApplicationDbContext db, IPlatformHealthChecker healthChecker)
    : IRequestHandler<GetPublicStatusQuery, PublicStatusDto>
{
    public async Task<PublicStatusDto> Handle(GetPublicStatusQuery request, CancellationToken ct)
    {
        var health = await healthChecker.CheckAsync(ct);
        var components = health.Select(kv => new ComponentStatusDto(kv.Key, kv.Value)).OrderBy(c => c.Component).ToList();

        var overall = components.Select(c => c.Health).DefaultIfEmpty(ComponentHealth.Operational).Max();

        var incidents = await db.SystemIncidents.Include(i => i.Updates)
            .OrderByDescending(i => i.StartedAt)
            .Take(10)
            .ToListAsync(ct);

        return new PublicStatusDto(overall, components, incidents.Select(ListIncidentsQueryHandler.ToDto).ToList());
    }
}
