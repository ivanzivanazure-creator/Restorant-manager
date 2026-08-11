using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.RestaurantOps;

namespace RestaurantSaaS.Application.Onboarding;

public sealed record OnboardingStepDto(string Key, string Label, bool IsComplete, string ActionRoute);

public sealed record OnboardingStatusDto(bool IsComplete, IReadOnlyCollection<OnboardingStepDto> Steps);

/// <summary>Drives the dashboard's onboarding checklist. Deliberately computed from existing data (has a
/// location? a menu item? a table? a connected payment account?) rather than a separate "wizard progress"
/// table — there's nothing to get out of sync, and completing the underlying action always completes the
/// step even if the tenant skipped the checklist UI entirely.</summary>
public sealed record GetOnboardingStatusQuery(Guid TenantId) : IRequest<OnboardingStatusDto>, ITenantScopedRequest;

public sealed class GetOnboardingStatusQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken ct)
    {
        var hasLocation = await db.Locations.AnyAsync(l => l.TenantId == request.TenantId, ct);
        var hasMenuItem = await db.Set<Product>().AnyAsync(p => p.TenantId == request.TenantId, ct);
        var hasTable = await db.Set<Table>().AnyAsync(t => t.TenantId == request.TenantId, ct);
        var hasEmployee = await db.Employees.AnyAsync(e => e.TenantId == request.TenantId, ct);
        var tenant = await db.RestaurantOwners.SingleAsync(t => t.Id == request.TenantId, ct);

        var steps = new List<OnboardingStepDto>
        {
            new("location", "Add your first location", hasLocation, "/restaurant-management"),
            new("menu", "Build your menu", hasMenuItem, "/menu"),
            new("tables", "Set up your tables", hasTable, "/restaurant-management"),
            new("staff", "Invite your team", hasEmployee, "/restaurant-management"),
            new("payments", "Connect a payment account", tenant.StripeOnboardingComplete, "/billing"),
        };

        return new OnboardingStatusDto(steps.All(s => s.IsComplete), steps);
    }
}
