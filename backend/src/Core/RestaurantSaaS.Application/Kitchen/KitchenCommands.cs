using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Pos;

namespace RestaurantSaaS.Application.Kitchen;

public sealed record StartKitchenTicketCommand(Guid TenantId, Guid TicketId) : IRequest, ITenantScopedRequest;

public sealed class StartKitchenTicketCommandHandler(IApplicationDbContext db, IRealtimeNotifier realtime) : IRequestHandler<StartKitchenTicketCommand>
{
    public async Task Handle(StartKitchenTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.KitchenTickets.SingleOrDefaultAsync(t => t.Id == request.TicketId && t.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.TicketId);

        ticket.Start();
        await db.SaveChangesAsync(ct);

        await realtime.NotifyKitchenAsync(ticket.LocationId, new { @event = "ticket-started", ticketId = ticket.Id }, ct);
    }
}

public sealed record MarkKitchenTicketReadyCommand(Guid TenantId, Guid TicketId) : IRequest, ITenantScopedRequest;

public sealed class MarkKitchenTicketReadyCommandHandler(IApplicationDbContext db, IRealtimeNotifier realtime) : IRequestHandler<MarkKitchenTicketReadyCommand>
{
    public async Task Handle(MarkKitchenTicketReadyCommand request, CancellationToken ct)
    {
        var ticket = await db.KitchenTickets.SingleOrDefaultAsync(t => t.Id == request.TicketId && t.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.TicketId);

        ticket.MarkReady();

        var order = await db.Orders.SingleAsync(o => o.Id == ticket.OrderId, ct);
        order.MarkReadyToServe(); // raises OrderReadyToServeEvent -> pushes to OrdersHub for the waiter

        await db.SaveChangesAsync(ct);

        await realtime.NotifyKitchenAsync(ticket.LocationId, new { @event = "ticket-ready", ticketId = ticket.Id, orderId = order.Id }, ct);
    }
}

public sealed record MarkKitchenTicketServedCommand(Guid TenantId, Guid TicketId) : IRequest, ITenantScopedRequest;

public sealed class MarkKitchenTicketServedCommandHandler(IApplicationDbContext db) : IRequestHandler<MarkKitchenTicketServedCommand>
{
    public async Task Handle(MarkKitchenTicketServedCommand request, CancellationToken ct)
    {
        var ticket = await db.KitchenTickets.SingleOrDefaultAsync(t => t.Id == request.TicketId && t.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.TicketId);

        ticket.MarkServed();
        await db.SaveChangesAsync(ct);
    }
}

public sealed record SetKitchenTicketPriorityCommand(Guid TenantId, Guid TicketId, KitchenTicketPriority Priority) : IRequest, ITenantScopedRequest;

public sealed class SetKitchenTicketPriorityCommandHandler(IApplicationDbContext db, IRealtimeNotifier realtime) : IRequestHandler<SetKitchenTicketPriorityCommand>
{
    public async Task Handle(SetKitchenTicketPriorityCommand request, CancellationToken ct)
    {
        var ticket = await db.KitchenTickets.SingleOrDefaultAsync(t => t.Id == request.TicketId && t.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(KitchenTicket), request.TicketId);

        ticket.SetPriority(request.Priority);
        await db.SaveChangesAsync(ct);

        await realtime.NotifyKitchenAsync(ticket.LocationId, new { @event = "ticket-priority-changed", ticketId = ticket.Id, priority = ticket.Priority.ToString() }, ct);
    }
}
