using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Identity;

public static class ClaimTypesExt
{
    public const string TenantId = "tenant_id";
    public const string LocationId = "location_id";
    public const string SuperAdmin = "super_admin";
    public const string Permission = "permission";
}

public sealed class HttpContextTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsSuperAdmin => User?.HasClaim(ClaimTypesExt.SuperAdmin, "true") ?? false;

    public Guid? TenantId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypesExt.TenantId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? LocationId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypesExt.LocationId)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}

public sealed class HttpContextCurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll(ClaimTypesExt.Permission).Select(c => c.Value).ToArray() ?? [];

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
