using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Tenancy;
using RestaurantSaaS.Domain.ValueObjects;

namespace RestaurantSaaS.Application.RestaurantManagement;

public sealed record RestaurantDto(Guid Id, string Name, string LegalName, string DefaultCurrency, bool IsActive);
public sealed record LocationDto(Guid Id, Guid RestaurantId, string Name, string City, string Country, string Currency, bool IsActive);

public sealed record CreateRestaurantCommand(Guid TenantId, string Name, string LegalName, string DefaultCurrency)
    : IRequest<RestaurantDto>, ITenantScopedRequest;

public sealed class CreateRestaurantCommandValidator : AbstractValidator<CreateRestaurantCommand>
{
    public CreateRestaurantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
    }
}

public sealed class CreateRestaurantCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateRestaurantCommand, RestaurantDto>
{
    public async Task<RestaurantDto> Handle(CreateRestaurantCommand request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleOrDefaultAsync(t => t.Id == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(RestaurantOwner), request.TenantId);

        var restaurant = tenant.AddRestaurant(request.Name, request.LegalName, request.DefaultCurrency);
        await db.SaveChangesAsync(ct);

        return new RestaurantDto(restaurant.Id, restaurant.Name, restaurant.LegalName, restaurant.DefaultCurrency, restaurant.IsActive);
    }
}

public sealed record CreateLocationCommand(
    Guid TenantId, Guid RestaurantId, string Name, string AddressLine1, string City, string Country, string Currency)
    : IRequest<LocationDto>, ITenantScopedRequest;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class CreateLocationCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateLocationCommand, LocationDto>
{
    public async Task<LocationDto> Handle(CreateLocationCommand request, CancellationToken ct)
    {
        var restaurant = await db.Restaurants.SingleOrDefaultAsync(r => r.Id == request.RestaurantId && r.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Domain.Tenancy.Restaurant), request.RestaurantId);

        var location = restaurant.AddLocation(request.Name, request.AddressLine1, request.City, request.Country, request.Currency);
        await db.SaveChangesAsync(ct);

        return new LocationDto(location.Id, restaurant.Id, location.Name, request.City, request.Country, location.Currency, location.IsActive);
    }
}

public sealed record WorkingHourInput(DayOfWeek DayOfWeek, TimeOnly? Start, TimeOnly? End, bool IsClosed);

public sealed record UpdateWorkingHoursCommand(Guid TenantId, Guid LocationId, IReadOnlyCollection<WorkingHourInput> Hours)
    : IRequest, ITenantScopedRequest;

public sealed class UpdateWorkingHoursCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateWorkingHoursCommand>
{
    public async Task Handle(UpdateWorkingHoursCommand request, CancellationToken ct)
    {
        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId && l.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Location), request.LocationId);

        var hours = request.Hours.Select(h => new WorkingHour(
            location.Id, h.DayOfWeek,
            new TimeRange(h.Start ?? TimeOnly.MinValue, h.End ?? TimeOnly.MaxValue),
            h.IsClosed));

        location.SetWorkingHours(hours);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record UpdateTaxConfigCommand(Guid TenantId, Guid LocationId, decimal TaxRatePercent, string TaxLabel, bool PricesIncludeTax)
    : IRequest, ITenantScopedRequest;

public sealed class UpdateTaxConfigCommandValidator : AbstractValidator<UpdateTaxConfigCommand>
{
    public UpdateTaxConfigCommandValidator() => RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100);
}

public sealed class UpdateTaxConfigCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateTaxConfigCommand>
{
    public async Task Handle(UpdateTaxConfigCommand request, CancellationToken ct)
    {
        var location = await db.Locations.Include(l => l.TaxConfig)
            .SingleOrDefaultAsync(l => l.Id == request.LocationId && l.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Location), request.LocationId);

        location.TaxConfig.Update(request.TaxRatePercent, request.TaxLabel, request.PricesIncludeTax);
        await db.SaveChangesAsync(ct);
    }
}
