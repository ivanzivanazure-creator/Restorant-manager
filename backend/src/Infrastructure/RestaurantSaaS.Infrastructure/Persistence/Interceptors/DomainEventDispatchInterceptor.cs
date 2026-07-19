using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RestaurantSaaS.Application.Common.Events;
using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Infrastructure.Persistence.Interceptors;

/// <summary>Publishes each aggregate's queued domain events via MediatR after a successful SaveChanges,
/// then clears them. Handlers (e.g. real-time SignalR pushes, notification dispatch) run outside the DB
/// transaction so a slow subscriber can never roll back the write.</summary>
public sealed class DomainEventDispatchInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        await DispatchEventsAsync(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private async Task DispatchEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
                await publisher.Publish(notification, ct);
            }
        }
    }
}
