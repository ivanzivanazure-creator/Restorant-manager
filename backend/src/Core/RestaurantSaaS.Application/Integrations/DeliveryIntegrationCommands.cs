using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Integrations;

namespace RestaurantSaaS.Application.Integrations;

public sealed record DeliveryIntegrationDto(Guid Id, Guid LocationId, DeliveryPlatform Platform, string? ExternalStoreId, bool IsActive, DateTimeOffset? LastOrderReceivedAt);

/// <summary>Registers a tenant's connection to a delivery platform and returns the plaintext webhook
/// secret exactly once (only the hash is persisted) — the tenant pastes it into the platform's own
/// integration settings (UberEats/DoorDash merchant portal) alongside the webhook URL.</summary>
public sealed record RegisterDeliveryIntegrationCommand(Guid TenantId, Guid LocationId, DeliveryPlatform Platform, string? ExternalStoreId)
    : IRequest<(DeliveryIntegrationDto Integration, string WebhookSecret)>, ITenantScopedRequest;

public sealed class RegisterDeliveryIntegrationCommandValidator : AbstractValidator<RegisterDeliveryIntegrationCommand>
{
    public RegisterDeliveryIntegrationCommandValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty();
    }
}

public sealed class RegisterDeliveryIntegrationCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RegisterDeliveryIntegrationCommand, (DeliveryIntegrationDto, string)>
{
    public async Task<(DeliveryIntegrationDto, string)> Handle(RegisterDeliveryIntegrationCommand request, CancellationToken ct)
    {
        var locationExists = await db.Locations.AnyAsync(l => l.Id == request.LocationId && l.TenantId == request.TenantId, ct);
        if (!locationExists) throw new NotFoundException("Location", request.LocationId);

        var secret = GenerateSecret();
        var integration = new DeliveryIntegration(request.TenantId, request.LocationId, request.Platform, Hash(secret), request.ExternalStoreId);

        db.DeliveryIntegrations.Add(integration);
        await db.SaveChangesAsync(ct);

        return (ToDto(integration), secret);
    }

    internal static string GenerateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    internal static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    internal static DeliveryIntegrationDto ToDto(DeliveryIntegration i) =>
        new(i.Id, i.LocationId, i.Platform, i.ExternalStoreId, i.IsActive, i.LastOrderReceivedAt);
}

public sealed record ListDeliveryIntegrationsQuery(Guid TenantId) : IRequest<IReadOnlyCollection<DeliveryIntegrationDto>>, ITenantScopedRequest;

public sealed class ListDeliveryIntegrationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ListDeliveryIntegrationsQuery, IReadOnlyCollection<DeliveryIntegrationDto>>
{
    public async Task<IReadOnlyCollection<DeliveryIntegrationDto>> Handle(ListDeliveryIntegrationsQuery request, CancellationToken ct) =>
        await db.DeliveryIntegrations
            .Where(i => i.TenantId == request.TenantId)
            .Select(i => new DeliveryIntegrationDto(i.Id, i.LocationId, i.Platform, i.ExternalStoreId, i.IsActive, i.LastOrderReceivedAt))
            .ToListAsync(ct);
}

public sealed record DeactivateDeliveryIntegrationCommand(Guid TenantId, Guid IntegrationId) : IRequest, ITenantScopedRequest;

public sealed class DeactivateDeliveryIntegrationCommandHandler(IApplicationDbContext db) : IRequestHandler<DeactivateDeliveryIntegrationCommand>
{
    public async Task Handle(DeactivateDeliveryIntegrationCommand request, CancellationToken ct)
    {
        var integration = await db.DeliveryIntegrations.SingleOrDefaultAsync(i => i.Id == request.IntegrationId && i.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(DeliveryIntegration), request.IntegrationId);

        integration.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
