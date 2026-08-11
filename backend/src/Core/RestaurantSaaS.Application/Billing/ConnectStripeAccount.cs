using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Tenancy;

namespace RestaurantSaaS.Application.Billing;

public sealed record ConnectStripeAccountResultDto(string OnboardingUrl);

/// <summary>Kicks off (or resumes) Stripe Connect onboarding for the calling tenant. Idempotent: reuses
/// the existing connected account if one was already created, only generating a fresh onboarding link
/// (Stripe's own links expire after a few minutes).</summary>
public sealed record ConnectStripeAccountCommand(Guid TenantId, string ReturnUrl, string RefreshUrl)
    : IRequest<ConnectStripeAccountResultDto>, ITenantScopedRequest;

public sealed class ConnectStripeAccountCommandHandler(IApplicationDbContext db, IPaymentGatewayService paymentGateway)
    : IRequestHandler<ConnectStripeAccountCommand, ConnectStripeAccountResultDto>
{
    public async Task<ConnectStripeAccountResultDto> Handle(ConnectStripeAccountCommand request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleOrDefaultAsync(t => t.Id == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(RestaurantOwner), request.TenantId);

        if (tenant.StripeConnectedAccountId is null)
        {
            var accountId = await paymentGateway.CreateConnectedAccountAsync(tenant.ContactEmail, tenant.CompanyName, ct);
            tenant.AttachStripeConnectedAccount(accountId);
            await db.SaveChangesAsync(ct);
        }

        var onboardingUrl = await paymentGateway.CreateAccountOnboardingLinkAsync(
            tenant.StripeConnectedAccountId!, request.ReturnUrl, request.RefreshUrl, ct);

        return new ConnectStripeAccountResultDto(onboardingUrl);
    }
}

public sealed record StripeAccountStatusDto(bool IsConnected, bool OnboardingComplete);

public sealed record GetStripeAccountStatusQuery(Guid TenantId) : IRequest<StripeAccountStatusDto>, ITenantScopedRequest;

public sealed class GetStripeAccountStatusQueryHandler(IApplicationDbContext db) : IRequestHandler<GetStripeAccountStatusQuery, StripeAccountStatusDto>
{
    public async Task<StripeAccountStatusDto> Handle(GetStripeAccountStatusQuery request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleOrDefaultAsync(t => t.Id == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(RestaurantOwner), request.TenantId);

        return new StripeAccountStatusDto(tenant.StripeConnectedAccountId is not null, tenant.StripeOnboardingComplete);
    }
}

/// <summary>Called from a Stripe webhook (account.updated) once KYC/onboarding finishes — see
/// Api/Controllers/BillingController's webhook endpoint. Kept separate from the connect flow itself
/// since it's driven by Stripe, not the tenant's own request.</summary>
public sealed record MarkStripeOnboardingCompleteCommand(string StripeConnectedAccountId) : IRequest;

public sealed class MarkStripeOnboardingCompleteCommandHandler(IApplicationDbContext db) : IRequestHandler<MarkStripeOnboardingCompleteCommand>
{
    public async Task Handle(MarkStripeOnboardingCompleteCommand request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleOrDefaultAsync(t => t.StripeConnectedAccountId == request.StripeConnectedAccountId, ct);
        tenant?.MarkStripeOnboardingComplete();
        if (tenant is not null) await db.SaveChangesAsync(ct);
    }
}
