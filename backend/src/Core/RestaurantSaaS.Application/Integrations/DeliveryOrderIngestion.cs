using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Application.Pos;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Integrations;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Application.Integrations;

public sealed record DeliveryOrderItemPayload(string ProductName, int Quantity, string? Notes);

/// <summary>Generic shape every delivery-platform adapter normalizes its own webhook payload into before
/// calling this command. Today only product-name matching against the location's active menu is
/// implemented (no SKU mapping table yet) — items that don't match by name are skipped and reported back
/// in SkippedItemNames rather than silently dropped or guessed at. Per-platform payload parsers
/// (UberEats' and DoorDash's webhook JSON shapes differ) are Phase 2 — see docs/ROADMAP.md.</summary>
public sealed record IngestDeliveryOrderCommand(
    Guid LocationId, DeliveryPlatform Platform, string WebhookSecret, string ExternalOrderId, IReadOnlyCollection<DeliveryOrderItemPayload> Items)
    : IRequest<IngestDeliveryOrderResultDto>;

public sealed record IngestDeliveryOrderResultDto(Guid OrderId, int MatchedItemCount, IReadOnlyCollection<string> SkippedItemNames, bool SentToKitchen);

public sealed class IngestDeliveryOrderCommandValidator : AbstractValidator<IngestDeliveryOrderCommand>
{
    public IngestDeliveryOrderCommandValidator()
    {
        RuleFor(x => x.WebhookSecret).NotEmpty();
        RuleFor(x => x.ExternalOrderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}

public sealed class IngestDeliveryOrderCommandHandler(IApplicationDbContext db, OrderKitchenDispatchService dispatchService)
    : IRequestHandler<IngestDeliveryOrderCommand, IngestDeliveryOrderResultDto>
{
    public async Task<IngestDeliveryOrderResultDto> Handle(IngestDeliveryOrderCommand request, CancellationToken ct)
    {
        var integration = await db.DeliveryIntegrations
            .SingleOrDefaultAsync(i => i.LocationId == request.LocationId && i.Platform == request.Platform && i.IsActive, ct)
            ?? throw new NotFoundException(nameof(DeliveryIntegration), $"{request.LocationId}/{request.Platform}");

        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.WebhookSecret)));
        if (secretHash != integration.WebhookSecretHash)
            throw new UnauthorizedAccessException("Invalid delivery integration webhook secret.");

        var location = await db.Locations.SingleAsync(l => l.Id == request.LocationId, ct);
        var tenant = await db.RestaurantOwners.SingleAsync(t => t.Id == location.TenantId, ct);

        var activeProducts = await db.Products
            .Where(p => p.TenantId == location.TenantId && p.IsActive)
            .Include(p => p.Variants)
            .ToListAsync(ct);

        var order = new Order(location.TenantId, location.Id, tableId: null, serverEmployeeId: tenant.PrimaryUserId,
            location.Currency, location.TaxConfig.DefaultTaxRatePercent, OrderSource.ThirdPartyDelivery);
        order.AttachDeliverySource(request.Platform, request.ExternalOrderId);

        var skipped = new List<string>();
        foreach (var itemPayload in request.Items)
        {
            var product = activeProducts.SingleOrDefault(p => string.Equals(p.Name, itemPayload.ProductName, StringComparison.OrdinalIgnoreCase));
            var variant = product?.Variants.FirstOrDefault(v => v.IsDefault) ?? product?.Variants.FirstOrDefault();
            if (product is null || variant is null)
            {
                skipped.Add(itemPayload.ProductName);
                continue;
            }

            order.AddItem(variant.Id, product.Name, variant.Name, Money.Of(product.BasePrice.Amount + variant.PriceDelta, product.BasePrice.Currency),
                itemPayload.Quantity, itemPayload.Notes);
        }

        db.Orders.Add(order);
        integration.RecordOrderReceived();
        await db.SaveChangesAsync(ct);

        var sentToKitchen = false;
        if (order.Items.Count > 0)
        {
            var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.LocationId == location.Id && w.IsActive, ct);
            if (warehouse is not null)
            {
                await dispatchService.DispatchAsync(order, warehouse.Id, targetCookMinutes: 20, ct);
                sentToKitchen = true;
            }
        }

        return new IngestDeliveryOrderResultDto(order.Id, order.Items.Count, skipped, sentToKitchen);
    }
}
