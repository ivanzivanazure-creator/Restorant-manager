using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Kitchen;

public sealed record KitchenTicketItemDto(Guid Id, string ProductName, string VariantName, int Quantity, string? Notes, KitchenTicketStatus Status);

public sealed record KitchenTicketDto(
    Guid Id, Guid OrderId, string? TableLabel, KitchenTicketStatus Status, KitchenTicketPriority Priority,
    int TargetCookMinutes, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, bool IsOverdue,
    IReadOnlyCollection<KitchenTicketItemDto> Items);

public sealed record GetKitchenQueueQuery(Guid TenantId, Guid LocationId) : IRequest<IReadOnlyCollection<KitchenTicketDto>>, ITenantScopedRequest;

public sealed class GetKitchenQueueQueryHandler(IApplicationDbContext db) : IRequestHandler<GetKitchenQueueQuery, IReadOnlyCollection<KitchenTicketDto>>
{
    private static readonly KitchenTicketStatus[] ActiveStatuses = [KitchenTicketStatus.Queued, KitchenTicketStatus.InProgress, KitchenTicketStatus.Ready];

    public async Task<IReadOnlyCollection<KitchenTicketDto>> Handle(GetKitchenQueueQuery request, CancellationToken ct)
    {
        var tickets = await db.KitchenTickets.Include(t => t.Items)
            .Where(t => t.LocationId == request.LocationId && t.TenantId == request.TenantId && ActiveStatuses.Contains(t.Status))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.QueuedAt)
            .ToListAsync(ct);

        return tickets.Select(t => new KitchenTicketDto(
            t.Id, t.OrderId, t.TableLabel, t.Status, t.Priority, t.TargetCookMinutes, t.QueuedAt, t.StartedAt, t.IsOverdue,
            t.Items.Select(i => new KitchenTicketItemDto(i.Id, i.ProductName, i.VariantName, i.Quantity, i.Notes, i.Status)).ToList()
        )).ToList();
    }
}
