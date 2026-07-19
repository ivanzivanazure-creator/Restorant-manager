using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Identity;

/// <summary>
/// Application-defined role, distinct from ASP.NET Identity's role table so tenants can see a
/// friendly, permission-driven role model. Seeded system roles: SuperAdmin, Owner, Manager,
/// Waiter, Chef, Cashier, InventoryClerk, HR, FrontDesk (hotel).
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public bool IsSystemRole { get; private set; }
    public Guid? TenantId { get; private set; } // null for system/global roles, set for tenant-custom roles

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    public Role(string name, bool isSystemRole, Guid? tenantId = null)
    {
        Name = name;
        IsSystemRole = isSystemRole;
        TenantId = tenantId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Grant(Permission permission)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id)) return;
        _rolePermissions.Add(new RolePermission(Id, permission.Id));
    }

    public void Revoke(Guid permissionId) => _rolePermissions.RemoveAll(rp => rp.PermissionId == permissionId);
}

/// <summary>Fine-grained permission, e.g. "pos.orders.create", "inventory.stock.adjust".</summary>
public class Permission : BaseEntity
{
    public string Key { get; private set; } = default!;
    public string Module { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    private Permission() { }

    public Permission(string key, string module, string description)
    {
        Key = key;
        Module = module;
        Description = description;
    }
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}

/// <summary>Maps an ASP.NET Identity user (Infrastructure) to one or more application Roles.</summary>
public class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? LocationId { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId, Guid? tenantId, Guid? locationId)
    {
        UserId = userId;
        RoleId = roleId;
        TenantId = tenantId;
        LocationId = locationId;
    }
}

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string DeviceInfo { get; private set; } = default!;
    public string CreatedByIp { get; private set; } = default!;

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, string deviceInfo, string createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        CreatedByIp = createdByIp;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

public class MfaEnrollment : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string EncryptedSecret { get; private set; } = default!;
    public bool IsEnabled { get; private set; }
    public IReadOnlyCollection<string> RecoveryCodeHashes { get; private set; } = [];

    private MfaEnrollment() { }

    public MfaEnrollment(Guid userId, string encryptedSecret, IReadOnlyCollection<string> recoveryCodeHashes)
    {
        UserId = userId;
        EncryptedSecret = encryptedSecret;
        RecoveryCodeHashes = recoveryCodeHashes;
        IsEnabled = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate() => IsEnabled = true;
    public void Deactivate() => IsEnabled = false;

    public void ConsumeRecoveryCode(string hash) =>
        RecoveryCodeHashes = RecoveryCodeHashes.Where(c => c != hash).ToList();
}
