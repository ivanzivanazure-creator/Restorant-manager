using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Domain.Subscription;
using RestaurantSaaS.Domain.Tenancy;
using ValidationException = RestaurantSaaS.Application.Common.Exceptions.ValidationException;

namespace RestaurantSaaS.Application.Auth;

/// <summary>Self-service sign-up: creates the RestaurantOwner tenant, the primary Owner user account,
/// grants the system "Owner" role, and starts a 14-day free trial on the given package.</summary>
public sealed record RegisterOwnerCommand(
    string CompanyName, string Email, string Password, string FirstName, string LastName, string PackageName)
    : IRequest<AuthTokensDto>;

public sealed class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    public RegisterOwnerCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PackageName).NotEmpty();
    }
}

public sealed class RegisterOwnerCommandHandler(
    IApplicationDbContext db, IIdentityService identityService, ICurrentUserService currentUser, TokenIssuer tokenIssuer)
    : IRequestHandler<RegisterOwnerCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RegisterOwnerCommand request, CancellationToken ct)
    {
        if (await identityService.FindByEmailAsync(request.Email, ct) is not null)
            throw new ConflictException($"An account with email '{request.Email}' already exists.");

        var package = await db.Packages.SingleOrDefaultAsync(p => p.Name == request.PackageName && p.IsActive, ct)
            ?? throw new NotFoundException(nameof(Package), request.PackageName);

        var (succeeded, userId, errors) = await identityService.CreateUserAsync(
            request.Email, request.Password, request.FirstName, request.LastName, ct);
        if (!succeeded) throw new ValidationException(errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.Password), e)));

        var tenant = new RestaurantOwner(request.CompanyName, request.Email, userId);
        db.RestaurantOwners.Add(tenant);

        var subscription = new TenantSubscription(tenant.Id, package.Id, BillingCycle.Monthly);
        db.Set<TenantSubscription>().Add(subscription);

        await identityService.AssignTenantAsync(userId, tenant.Id, null, ct);

        var ownerRole = await db.Roles.SingleAsync(r => r.TenantId == null && r.Name == "Owner", ct);
        db.Set<UserRole>().Add(new UserRole(userId, ownerRole.Id, tenant.Id, null));

        await db.SaveChangesAsync(ct);

        var user = new UserAccountDto(userId, request.Email, request.FirstName, request.LastName,
            IsSuperAdmin: false, tenant.Id, DefaultLocationId: null, IsActive: true, MfaEnabled: false);

        return await tokenIssuer.IssueAsync(user, "registration", currentUser.IpAddress ?? "unknown", ct);
    }
}
