using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;

namespace RestaurantSaaS.Application.SuperAdmin;

public sealed record TenantSummaryDto(
    Guid Id, string CompanyName, string ContactEmail, TenantStatus Status,
    string PackageName, SubscriptionStatus SubscriptionStatus, DateTimeOffset CurrentPeriodEnd, int RestaurantCount);

public sealed record ListTenantsQuery(int PageNumber = 1, int PageSize = 20, string? Search = null) : IRequest<Common.Models.PaginatedList<TenantSummaryDto>>;

public sealed class ListTenantsQueryHandler(IApplicationDbContext db) : IRequestHandler<ListTenantsQuery, Common.Models.PaginatedList<TenantSummaryDto>>
{
    public async Task<Common.Models.PaginatedList<TenantSummaryDto>> Handle(ListTenantsQuery request, CancellationToken ct)
    {
        var query =
            from tenant in db.RestaurantOwners
            join sub in db.Set<TenantSubscription>() on tenant.Id equals sub.TenantId
            join pkg in db.Packages on sub.PackageId equals pkg.Id
            where request.Search == null || tenant.CompanyName.Contains(request.Search) || tenant.ContactEmail.Contains(request.Search)
            select new TenantSummaryDto(tenant.Id, tenant.CompanyName, tenant.ContactEmail, tenant.Status,
                pkg.Name, sub.Status, sub.CurrentPeriodEnd, tenant.Restaurants.Count);

        return await Common.Models.PaginatedList<TenantSummaryDto>.CreateAsync(query, request.PageNumber, request.PageSize, ct);
    }
}

public sealed record ActivateTenantSubscriptionCommand(Guid TenantId, string StripeCustomerId, string StripeSubscriptionId, DateTimeOffset PeriodEnd)
    : IRequest;

public sealed class ActivateTenantSubscriptionCommandValidator : AbstractValidator<ActivateTenantSubscriptionCommand>
{
    public ActivateTenantSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PeriodEnd).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class ActivateTenantSubscriptionCommandHandler(IApplicationDbContext db) : IRequestHandler<ActivateTenantSubscriptionCommand>
{
    public async Task Handle(ActivateTenantSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await db.Set<TenantSubscription>().SingleOrDefaultAsync(s => s.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(TenantSubscription), request.TenantId);

        subscription.Activate(request.StripeCustomerId, request.StripeSubscriptionId, request.PeriodEnd);

        var tenant = await db.RestaurantOwners.SingleAsync(t => t.Id == request.TenantId, ct);
        tenant.Activate();

        await db.SaveChangesAsync(ct);
    }
}

public sealed record DeactivateTenantCommand(Guid TenantId) : IRequest;

public sealed class DeactivateTenantCommandHandler(IApplicationDbContext db) : IRequestHandler<DeactivateTenantCommand>
{
    public async Task Handle(DeactivateTenantCommand request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleOrDefaultAsync(t => t.Id == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(RestaurantOwner), request.TenantId);

        tenant.Suspend();

        var subscription = await db.Set<TenantSubscription>().SingleOrDefaultAsync(s => s.TenantId == request.TenantId, ct);
        subscription?.Cancel();

        await db.SaveChangesAsync(ct);
    }
}

public sealed record PlatformAnalyticsDto(
    int TotalTenants, int ActiveTenants, int TrialingTenants, int LockedTenants, decimal MonthlyRecurringRevenue);

public sealed record GetPlatformAnalyticsQuery : IRequest<PlatformAnalyticsDto>;

public sealed class GetPlatformAnalyticsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetPlatformAnalyticsQuery, PlatformAnalyticsDto>
{
    public async Task<PlatformAnalyticsDto> Handle(GetPlatformAnalyticsQuery request, CancellationToken ct)
    {
        var subs = await (
            from sub in db.Set<TenantSubscription>()
            join pkg in db.Packages on sub.PackageId equals pkg.Id
            select new { sub.Status, pkg.MonthlyPrice, pkg.YearlyPrice, sub.BillingCycle }).ToListAsync(ct);

        var totalTenants = await db.RestaurantOwners.CountAsync(ct);
        var active = subs.Count(s => s.Status == SubscriptionStatus.Active);
        var trialing = subs.Count(s => s.Status == SubscriptionStatus.Trialing);
        var locked = subs.Count(s => s.Status == SubscriptionStatus.Locked);
        var mrr = subs.Where(s => s.Status == SubscriptionStatus.Active)
            .Sum(s => s.BillingCycle == BillingCycle.Yearly ? s.YearlyPrice / 12m : s.MonthlyPrice);

        return new PlatformAnalyticsDto(totalTenants, active, trialing, locked, mrr);
    }
}
