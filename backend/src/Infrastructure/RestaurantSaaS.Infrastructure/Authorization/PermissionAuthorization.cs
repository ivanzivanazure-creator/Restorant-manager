using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RestaurantSaaS.Application.Common.Security;
using RestaurantSaaS.Infrastructure.Identity;

namespace RestaurantSaaS.Infrastructure.Authorization;

/// <summary>Resolves `[Authorize(Policy = Permissions.Pos.TakePayment)]` (or any key from
/// Permissions.All) into a requirement dynamically, so adding a new permission never requires
/// registering a new named policy at startup.</summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(Options.Create(new AuthorizationOptions()));

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (Permissions.All.Contains(policyName) || policyName == Permissions.SuperAdminAccess)
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // SuperAdmin bypasses every module permission check (but SuperAdminAccess itself still requires the claim).
        if (requirement.Permission != Permissions.SuperAdminAccess &&
            context.User.HasClaim(ClaimTypesExt.SuperAdmin, "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(ClaimTypesExt.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
