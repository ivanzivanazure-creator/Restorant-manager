using MediatR;
using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Application.Common.Events;

/// <summary>Wraps a zero-dependency Domain event so it can flow through the MediatR pipeline without
/// Domain itself taking a MediatR package reference. Infrastructure's DomainEventDispatchInterceptor
/// publishes one of these per queued domain event; handlers subscribe to the closed generic type.</summary>
public sealed class DomainEventNotification<TDomainEvent>(TDomainEvent domainEvent) : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; } = domainEvent;
}
