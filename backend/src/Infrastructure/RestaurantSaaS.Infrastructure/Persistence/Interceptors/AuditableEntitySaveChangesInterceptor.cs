using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUser, IDateTimeProvider dateTime) : SaveChangesInterceptor
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
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = dateTime.UtcNow;
                entry.Entity.CreatedBy = currentUser.UserId;
            }

            if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.ModifiedAt = dateTime.UtcNow;
                entry.Entity.ModifiedBy = currentUser.UserId;
            }
        }
    }
}

file static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is not null && r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State is EntityState.Added or EntityState.Modified);
}
