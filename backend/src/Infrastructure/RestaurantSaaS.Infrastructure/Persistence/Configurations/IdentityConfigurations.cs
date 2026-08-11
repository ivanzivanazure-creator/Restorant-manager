using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Identity;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("app_roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.HasMany(x => x.RolePermissions).WithOne().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("app_permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("app_role_permissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("app_user_roles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.RoleId, x.LocationId }).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

public class MfaEnrollmentConfiguration : IEntityTypeConfiguration<MfaEnrollment>
{
    public void Configure(EntityTypeBuilder<MfaEnrollment> builder)
    {
        builder.ToTable("mfa_enrollments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.EncryptedSecret).IsRequired();
        builder.Property(x => x.RecoveryCodeHashes)
            .HasConversion(JsonValueConverter<IReadOnlyCollection<string>>.Converter, JsonValueConverter<IReadOnlyCollection<string>>.Comparer)
            .HasColumnType("jsonb");
    }
}
