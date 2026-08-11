using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Infrastructure.Persistence.Interceptors;

/// <summary>Defense-in-depth for multi-tenancy: stamps TenantId on new entities and refuses to persist any
/// change to an entity whose TenantId doesn't match the caller's — a backstop in case a raw SQL/bulk path
/// or a missing global query filter would otherwise let a cross-tenant write through.</summary>
public sealed class TenantSaveChangesInterceptor(ITenantProvider tenantProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Apply(DbContext? context)
    {
        if (context is null || tenantProvider.IsSuperAdmin) return;

        foreach (var entry in context.ChangeTracker.Entries<IMustHaveTenant>())
        {
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity.TenantId == Guid.Empty:
                    SetTenantId(entry, tenantProvider.TenantId ?? throw new CrossTenantAccessException());
                    break;
                case EntityState.Added or EntityState.Modified or EntityState.Deleted
                    when tenantProvider.TenantId is not null && entry.Entity.TenantId != tenantProvider.TenantId:
                    throw new CrossTenantAccessException();
            }
        }
    }

    private static void SetTenantId(EntityEntry<IMustHaveTenant> entry, Guid tenantId) =>
        entry.Property(nameof(IMustHaveTenant.TenantId)).CurrentValue = tenantId;
}
