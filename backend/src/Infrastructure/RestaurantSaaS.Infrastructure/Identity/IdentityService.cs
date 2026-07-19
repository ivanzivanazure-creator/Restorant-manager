using Microsoft.AspNetCore.Identity;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<(bool Succeeded, Guid UserId, IReadOnlyCollection<string> Errors)> CreateUserAsync(
        string email, string password, string firstName, string lastName, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = false,
        };

        var result = await userManager.CreateAsync(user, password);
        return (result.Succeeded, user.Id, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<UserAccountDto?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<UserAccountDto?> FindByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsLockedOutAsync(user);
    }

    public async Task RecordLoginAttemptAsync(Guid userId, bool succeeded, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        if (succeeded) await userManager.ResetAccessFailedCountAsync(user);
        else await userManager.AccessFailedAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<(bool Succeeded, IReadOnlyCollection<string> Errors)> ResetPasswordAsync(
        Guid userId, string token, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return (false, ["User not found."]);

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task AssignTenantAsync(Guid userId, Guid tenantId, Guid? defaultLocationId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        user.TenantId = tenantId;
        user.DefaultLocationId = defaultLocationId;
        await userManager.UpdateAsync(user);
    }

    public async Task SetMfaEnabledAsync(Guid userId, bool enabled, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        user.TwoFactorEnabled = enabled;
        await userManager.UpdateAsync(user);
    }

    private Task<UserAccountDto> ToDtoAsync(ApplicationUser user) => Task.FromResult(new UserAccountDto(
        user.Id, user.Email!, user.FirstName, user.LastName, user.IsSuperAdmin,
        user.TenantId, user.DefaultLocationId, user.IsActive, user.TwoFactorEnabled));
}
