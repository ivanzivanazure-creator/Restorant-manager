using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Procurement;

namespace RestaurantSaaS.Application.Inventory;

public sealed record PurchaseOrderLineInput(Guid IngredientId, decimal Quantity, decimal UnitPrice);

public sealed record CreatePurchaseOrderCommand(Guid TenantId, Guid SupplierId, Guid WarehouseId, Guid RequestedByEmployeeId, IReadOnlyCollection<PurchaseOrderLineInput> Lines)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator() => RuleFor(x => x.Lines).NotEmpty();
}

public sealed class CreatePurchaseOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = new PurchaseOrder(request.TenantId, request.SupplierId, request.WarehouseId, request.RequestedByEmployeeId);
        foreach (var line in request.Lines)
        {
            po.AddLine(line.IngredientId, line.Quantity, line.UnitPrice);
        }
        po.SubmitForApproval();

        db.Set<PurchaseOrder>().Add(po);
        await db.SaveChangesAsync(ct);
        return po.Id;
    }
}

public sealed record ApprovePurchaseOrderCommand(Guid TenantId, Guid PurchaseOrderId, Guid ApprovedByEmployeeId) : IRequest, ITenantScopedRequest;

public sealed class ApprovePurchaseOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<ApprovePurchaseOrderCommand>
{
    public async Task Handle(ApprovePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await db.Set<PurchaseOrder>().SingleOrDefaultAsync(p => p.Id == request.PurchaseOrderId && p.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

        po.Approve(request.ApprovedByEmployeeId);
        po.MarkOrdered();
        await db.SaveChangesAsync(ct);
    }
}

public sealed record ReceivePurchaseOrderCommand(Guid TenantId, Guid PurchaseOrderId, Guid ReceivedByEmployeeId, IReadOnlyCollection<(Guid LineId, decimal Quantity)> ReceivedLines)
    : IRequest, ITenantScopedRequest;

public sealed class ReceivePurchaseOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<ReceivePurchaseOrderCommand>
{
    public async Task Handle(ReceivePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await db.Set<PurchaseOrder>().Include(p => p.Lines)
            .SingleOrDefaultAsync(p => p.Id == request.PurchaseOrderId && p.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

        foreach (var (lineId, quantity) in request.ReceivedLines)
        {
            po.ReceiveLine(lineId, quantity);
            var line = po.Lines.Single(l => l.Id == lineId);

            var stock = await db.Set<Domain.Inventory.StockLevel>()
                .SingleOrDefaultAsync(s => s.WarehouseId == po.WarehouseId && s.IngredientId == line.IngredientId, ct);
            if (stock is null)
            {
                stock = new Domain.Inventory.StockLevel(request.TenantId, po.WarehouseId, line.IngredientId);
                db.Set<Domain.Inventory.StockLevel>().Add(stock);
            }
            stock.Receive(quantity, expiresAt: null, unitCost: line.UnitPrice);

            db.StockMovements.Add(new Domain.Inventory.StockMovement(request.TenantId, po.WarehouseId, line.IngredientId,
                Domain.Enums.StockMovementType.Receipt, quantity, reference: po.Id.ToString(), performedByEmployeeId: request.ReceivedByEmployeeId));
        }

        db.Set<GoodsReceipt>().Add(new GoodsReceipt(request.TenantId, po.Id, request.ReceivedByEmployeeId, notes: null));
        await db.SaveChangesAsync(ct);
    }
}
