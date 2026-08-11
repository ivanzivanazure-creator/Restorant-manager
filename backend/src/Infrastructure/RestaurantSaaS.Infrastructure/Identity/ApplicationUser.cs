using Microsoft.AspNetCore.Identity;

namespace RestaurantSaaS.Infrastructure.Identity;

/// <summary>ASP.NET Core Identity account. Authorization (roles/permissions) is modeled separately in
/// RestaurantSaaS.Domain.Identity (Role/Permission/UserRole) so the Domain doesn't depend on Identity packages;
/// this class only owns authentication concerns (password hash, lockout, MFA-enabled flag, etc.).</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public bool IsSuperAdmin { get; set; }
    public Guid? TenantId { get; set; } // set for Owner/Manager/Waiter/... accounts; null for SuperAdmin
    public Guid? DefaultLocationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}
