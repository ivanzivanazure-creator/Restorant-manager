using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Application.RestaurantManagement;

public sealed record GetRestaurantsQuery(Guid TenantId) : IRequest<IReadOnlyCollection<RestaurantDto>>, ITenantScopedRequest;

public sealed class GetRestaurantsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRestaurantsQuery, IReadOnlyCollection<RestaurantDto>>
{
    public async Task<IReadOnlyCollection<RestaurantDto>> Handle(GetRestaurantsQuery request, CancellationToken ct) =>
        await db.Restaurants
            .Where(r => r.TenantId == request.TenantId)
            .Select(r => new RestaurantDto(r.Id, r.Name, r.LegalName, r.DefaultCurrency, r.IsActive))
            .ToListAsync(ct);
}

public sealed record GetLocationsQuery(Guid TenantId, Guid? RestaurantId = null) : IRequest<IReadOnlyCollection<LocationDto>>, ITenantScopedRequest;

public sealed class GetLocationsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetLocationsQuery, IReadOnlyCollection<LocationDto>>
{
    public async Task<IReadOnlyCollection<LocationDto>> Handle(GetLocationsQuery request, CancellationToken ct) =>
        await db.Locations
            .Where(l => l.TenantId == request.TenantId && (request.RestaurantId == null || l.RestaurantId == request.RestaurantId))
            .Select(l => new LocationDto(l.Id, l.RestaurantId, l.Name, l.Address.City, l.Address.Country, l.Currency, l.IsActive))
            .ToListAsync(ct);
}
