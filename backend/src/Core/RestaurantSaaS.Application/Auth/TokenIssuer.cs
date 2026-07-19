using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Identity;

namespace RestaurantSaaS.Application.Auth;

/// <summary>Shared by Login/VerifyMfa/Refresh flows: resolves a user's current roles/permissions and
/// issues a fresh access+refresh token pair. Kept out of any single command handler so none of them
/// depend on another handler as a plain service.</summary>
public sealed class TokenIssuer(IApplicationDbContext db, IJwtTokenService jwtTokenService)
{
    public async Task<AuthTokensDto> IssueAsync(UserAccountDto user, string deviceInfo, string ipAddress, CancellationToken ct)
    {
        var (roles, permissions) = await ResolveRolesAndPermissionsAsync(user.Id, ct);

        var pair = jwtTokenService.GenerateTokenPair(
            user.Id, user.Email, user.TenantId, user.DefaultLocationId, roles, permissions, user.IsSuperAdmin,
            deviceInfo, ipAddress);

        await db.SaveChangesAsync(ct);

        return new AuthTokensDto(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAt);
    }

    private async Task<(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)> ResolveRolesAndPermissionsAsync(Guid userId, CancellationToken ct)
    {
        var roleIds = await db.Set<UserRole>().Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).Distinct().ToListAsync(ct);
        if (roleIds.Count == 0) return ([], []);

        var roles = await db.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);
        var roleIdSet = roles.Select(r => r.Id).ToHashSet();
        var permissionIds = await db.Set<RolePermission>().Where(rp => roleIdSet.Contains(rp.RoleId)).Select(rp => rp.PermissionId).Distinct().ToListAsync(ct);
        var permissionKeys = await db.Permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Key).ToListAsync(ct);
        return (roles.Select(r => r.Name).ToList(), permissionKeys);
    }
}
