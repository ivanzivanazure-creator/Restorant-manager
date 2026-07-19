using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.RestaurantOps;

namespace RestaurantSaaS.Application.RestaurantManagement;

public sealed record TableDto(Guid Id, string Label, int Capacity, TableShape Shape, TableStatus Status, float X, float Y, string? QrCodeImageDataUri);

public sealed record CreateTableCommand(Guid TenantId, Guid LocationId, string Label, int Capacity, TableShape Shape, float X, float Y)
    : IRequest<TableDto>, ITenantScopedRequest;

public sealed class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public sealed class CreateTableCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateTableCommand, TableDto>
{
    public async Task<TableDto> Handle(CreateTableCommand request, CancellationToken ct)
    {
        var locationExists = await db.Locations.AnyAsync(l => l.Id == request.LocationId && l.TenantId == request.TenantId, ct);
        if (!locationExists) throw new NotFoundException("Location", request.LocationId);

        var table = new Table(request.TenantId, request.LocationId, request.Label, request.Capacity, request.Shape, request.X, request.Y);
        db.Tables.Add(table);
        await db.SaveChangesAsync(ct);

        return ToDto(table, null);
    }

    internal static TableDto ToDto(Table table, string? qrImage) =>
        new(table.Id, table.Label, table.Capacity, table.Shape, table.Status, table.LayoutX, table.LayoutY, qrImage);
}

public sealed record TablePosition(Guid TableId, float X, float Y);

public sealed record UpdateTableLayoutCommand(Guid TenantId, Guid LocationId, IReadOnlyCollection<TablePosition> Positions)
    : IRequest, ITenantScopedRequest;

public sealed class UpdateTableLayoutCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateTableLayoutCommand>
{
    public async Task Handle(UpdateTableLayoutCommand request, CancellationToken ct)
    {
        var tableIds = request.Positions.Select(p => p.TableId).ToList();
        var tables = await db.Tables.Where(t => t.LocationId == request.LocationId && t.TenantId == request.TenantId && tableIds.Contains(t.Id)).ToListAsync(ct);

        var positionsById = request.Positions.ToDictionary(p => p.TableId);
        foreach (var table in tables)
        {
            var pos = positionsById[table.Id];
            table.MoveTo(pos.X, pos.Y);
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed record GenerateTableQrCodeCommand(Guid TenantId, Guid TableId, string SelfOrderBaseUrl) : IRequest<TableDto>, ITenantScopedRequest;

public sealed class GenerateTableQrCodeCommandHandler(IApplicationDbContext db, IQrCodeGenerator qrCodeGenerator)
    : IRequestHandler<GenerateTableQrCodeCommand, TableDto>
{
    public async Task<TableDto> Handle(GenerateTableQrCodeCommand request, CancellationToken ct)
    {
        var table = await db.Tables.SingleOrDefaultAsync(t => t.Id == request.TableId && t.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        var token = Guid.NewGuid().ToString("N");
        var payload = $"{request.SelfOrderBaseUrl.TrimEnd('/')}/order/{token}";
        var imageDataUri = qrCodeGenerator.GenerateDataUri(payload);

        table.AttachQrCode(new QrCode(table.Id, token, imageDataUri));
        await db.SaveChangesAsync(ct);

        return CreateTableCommandHandler.ToDto(table, imageDataUri);
    }
}

public sealed record GetLocationTablesQuery(Guid TenantId, Guid LocationId) : IRequest<IReadOnlyCollection<TableDto>>, ITenantScopedRequest;

public sealed class GetLocationTablesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetLocationTablesQuery, IReadOnlyCollection<TableDto>>
{
    public async Task<IReadOnlyCollection<TableDto>> Handle(GetLocationTablesQuery request, CancellationToken ct)
    {
        var tables = await db.Tables.Include(t => t.QrCode)
            .Where(t => t.LocationId == request.LocationId && t.TenantId == request.TenantId)
            .ToListAsync(ct);

        return tables.Select(t => CreateTableCommandHandler.ToDto(t, t.QrCode?.ImageUrl)).ToList();
    }
}
