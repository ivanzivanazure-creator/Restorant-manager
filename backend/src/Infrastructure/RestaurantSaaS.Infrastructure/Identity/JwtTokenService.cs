using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Identity;
using RestaurantSaaS.Infrastructure.Persistence;
using JwtSecurityToken = System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
using JwtSecurityTokenHandler = System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler;

namespace RestaurantSaaS.Infrastructure.Identity;

public sealed class JwtTokenService(
    ApplicationDbContext db,
    IOptions<JwtOptions> options,
    IDateTimeProvider dateTime) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public TokenPair GenerateTokenPair(Guid userId, string email, Guid? tenantId, Guid? locationId,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, bool isSuperAdmin,
        string deviceInfo, string ipAddress)
    {
        var accessToken = CreateAccessToken(userId, email, tenantId, locationId, roles, permissions, isSuperAdmin, out var expiresAt);
        var refreshTokenPlainText = GenerateSecureRandomToken();

        var refreshToken = new RefreshToken(
            userId,
            HashToken(refreshTokenPlainText),
            dateTime.UtcNow.AddDays(_options.RefreshTokenDays),
            deviceInfo,
            ipAddress);

        db.RefreshTokens.Add(refreshToken);
        // Caller (the command handler) is responsible for SaveChangesAsync as part of its unit of work.

        return new TokenPair(accessToken, refreshTokenPlainText, expiresAt);
    }

    public async Task<TokenPair> RefreshAsync(string refreshToken, string deviceInfo, string ipAddress, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var existing = await FindByHashAsync(hash, ct);

        if (existing is null || !existing.IsActive) throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await db.Users.FindAsync([existing.UserId], ct) ?? throw new UnauthorizedAccessException("User not found.");
        var (roles, permissions) = await RoleResolver.ResolveAsync(db, user.Id, ct);

        var newPair = GenerateTokenPair(user.Id, user.Email!, user.TenantId, user.DefaultLocationId, roles, permissions, user.IsSuperAdmin, deviceInfo, ipAddress);
        existing.Revoke(HashToken(newPair.RefreshToken));
        await db.SaveChangesAsync(ct);

        return newPair;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var existing = await FindByHashAsync(hash, ct);
        existing?.Revoke();
        await db.SaveChangesAsync(ct);
    }

    private Task<RefreshToken?> FindByHashAsync(string hash, CancellationToken ct) =>
        db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

    private string CreateAccessToken(Guid userId, string email, Guid? tenantId, Guid? locationId,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, bool isSuperAdmin, out DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNamesSub, userId.ToString()),
        };

        if (tenantId is not null) claims.Add(new Claim(ClaimTypesExt.TenantId, tenantId.Value.ToString()));
        if (locationId is not null) claims.Add(new Claim(ClaimTypesExt.LocationId, locationId.Value.ToString()));
        if (isSuperAdmin) claims.Add(new Claim(ClaimTypesExt.SuperAdmin, "true"));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(ClaimTypesExt.Permission, p)));

        expiresAt = dateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const string JwtRegisteredClaimNamesSub = "sub";

    private static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

/// <summary>Resolves a user's flattened role names + permission keys from the RBAC tables, used both at
/// login and on refresh so a permission change takes effect on the next token issuance.</summary>
internal static class RoleResolver
{
    public static async Task<(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions)> ResolveAsync(
        ApplicationDbContext db, Guid userId, CancellationToken ct)
    {
        var roleIds = await db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).Distinct().ToListAsync(ct);
        var roles = await db.Roles.Where(r => roleIds.Contains(r.Id)).Include(r => r.RolePermissions).ToListAsync(ct);
        var permissionIds = roles.SelectMany(r => r.RolePermissions).Select(rp => rp.PermissionId).Distinct().ToList();
        var permissions = await db.Permissions.Where(p => permissionIds.Contains(p.Id)).Select(p => p.Key).ToListAsync(ct);

        return (roles.Select(r => r.Name).ToList(), permissions);
    }
}
