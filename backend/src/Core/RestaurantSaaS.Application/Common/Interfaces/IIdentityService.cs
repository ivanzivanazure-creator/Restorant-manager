namespace RestaurantSaaS.Application.Common.Interfaces;

public sealed record UserAccountDto(Guid Id, string Email, string FirstName, string LastName,
    bool IsSuperAdmin, Guid? TenantId, Guid? DefaultLocationId, bool IsActive, bool MfaEnabled);

/// <summary>Application-facing abstraction over ASP.NET Core Identity (UserManager&lt;ApplicationUser&gt;),
/// implemented in Infrastructure. Keeps Application handlers free of any direct Identity package
/// dependency, and lets Application deal in plain DTOs/Guids instead of ApplicationUser.</summary>
public interface IIdentityService
{
    Task<(bool Succeeded, Guid UserId, IReadOnlyCollection<string> Errors)> CreateUserAsync(
        string email, string password, string firstName, string lastName, CancellationToken ct);

    Task<UserAccountDto?> FindByEmailAsync(string email, CancellationToken ct);
    Task<UserAccountDto?> FindByIdAsync(Guid userId, CancellationToken ct);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct);
    Task<bool> IsLockedOutAsync(Guid userId, CancellationToken ct);
    Task RecordLoginAttemptAsync(Guid userId, bool succeeded, CancellationToken ct);

    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct);
    Task<(bool Succeeded, IReadOnlyCollection<string> Errors)> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct);

    Task AssignTenantAsync(Guid userId, Guid tenantId, Guid? defaultLocationId, CancellationToken ct);
    Task SetMfaEnabledAsync(Guid userId, bool enabled, CancellationToken ct);
}
