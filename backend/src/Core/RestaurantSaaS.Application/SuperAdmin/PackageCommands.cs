using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Subscription;

namespace RestaurantSaaS.Application.SuperAdmin;

public sealed record PackageDto(Guid Id, string Name, int? MaxUsers, int MaxLocations, decimal MonthlyPrice, decimal YearlyPrice, bool IsActive);

public sealed record CreatePackageCommand(string Name, int? MaxUsers, int MaxLocations, decimal MonthlyPrice, decimal YearlyPrice)
    : IRequest<PackageDto>;

public sealed class CreatePackageCommandValidator : AbstractValidator<CreatePackageCommand>
{
    public CreatePackageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxLocations).GreaterThan(0);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.YearlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0).When(x => x.MaxUsers is not null);
    }
}

public sealed class CreatePackageCommandHandler(IApplicationDbContext db) : IRequestHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(CreatePackageCommand request, CancellationToken ct)
    {
        var package = new Package(request.Name, request.MaxUsers, request.MaxLocations, request.MonthlyPrice, request.YearlyPrice);
        db.Packages.Add(package);
        await db.SaveChangesAsync(ct);
        return new PackageDto(package.Id, package.Name, package.MaxUsers, package.MaxLocations, package.MonthlyPrice, package.YearlyPrice, package.IsActive);
    }
}

public sealed record ListPackagesQuery(bool ActiveOnly = true) : IRequest<IReadOnlyCollection<PackageDto>>;

public sealed class ListPackagesQueryHandler(IApplicationDbContext db) : IRequestHandler<ListPackagesQuery, IReadOnlyCollection<PackageDto>>
{
    public async Task<IReadOnlyCollection<PackageDto>> Handle(ListPackagesQuery request, CancellationToken ct)
    {
        var query = db.Packages.AsQueryable();
        if (request.ActiveOnly) query = query.Where(p => p.IsActive);

        return await query
            .Select(p => new PackageDto(p.Id, p.Name, p.MaxUsers, p.MaxLocations, p.MonthlyPrice, p.YearlyPrice, p.IsActive))
            .ToListAsync(ct);
    }
}
