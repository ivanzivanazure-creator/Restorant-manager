using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Status;

namespace RestaurantSaaS.Application.Status;

public sealed record IncidentUpdateDto(IncidentStatus Status, string Message, DateTimeOffset PostedAt);

public sealed record IncidentDto(
    Guid Id, string Title, string Description, IncidentSeverity Severity, IncidentStatus Status,
    IReadOnlyCollection<PlatformComponent> AffectedComponents, DateTimeOffset StartedAt, DateTimeOffset? ResolvedAt,
    IReadOnlyCollection<IncidentUpdateDto> Updates);

/// <summary>SuperAdmin-only: opens a new platform incident, shown immediately on the public status page.</summary>
public sealed record CreateIncidentCommand(string Title, string Description, IncidentSeverity Severity, IReadOnlyCollection<PlatformComponent> AffectedComponents)
    : IRequest<Guid>;

public sealed class CreateIncidentCommandValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.AffectedComponents).NotEmpty();
    }
}

public sealed class CreateIncidentCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateIncidentCommand, Guid>
{
    public async Task<Guid> Handle(CreateIncidentCommand request, CancellationToken ct)
    {
        var incident = new SystemIncident(request.Title, request.Description, request.Severity, request.AffectedComponents);
        db.SystemIncidents.Add(incident);
        await db.SaveChangesAsync(ct);
        return incident.Id;
    }
}

public sealed record PostIncidentUpdateCommand(Guid IncidentId, IncidentStatus Status, string Message) : IRequest;

public sealed class PostIncidentUpdateCommandValidator : AbstractValidator<PostIncidentUpdateCommand>
{
    public PostIncidentUpdateCommandValidator() => RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
}

public sealed class PostIncidentUpdateCommandHandler(IApplicationDbContext db) : IRequestHandler<PostIncidentUpdateCommand>
{
    public async Task Handle(PostIncidentUpdateCommand request, CancellationToken ct)
    {
        var incident = await db.SystemIncidents.Include(i => i.Updates).SingleOrDefaultAsync(i => i.Id == request.IncidentId, ct)
            ?? throw new NotFoundException(nameof(SystemIncident), request.IncidentId);

        incident.PostUpdate(request.Status, request.Message);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record ListIncidentsQuery(int Take = 20) : IRequest<IReadOnlyCollection<IncidentDto>>;

public sealed class ListIncidentsQueryHandler(IApplicationDbContext db) : IRequestHandler<ListIncidentsQuery, IReadOnlyCollection<IncidentDto>>
{
    public async Task<IReadOnlyCollection<IncidentDto>> Handle(ListIncidentsQuery request, CancellationToken ct)
    {
        var incidents = await db.SystemIncidents.Include(i => i.Updates)
            .OrderByDescending(i => i.StartedAt)
            .Take(request.Take)
            .ToListAsync(ct);

        return incidents.Select(ToDto).ToList();
    }

    internal static IncidentDto ToDto(SystemIncident i) => new(
        i.Id, i.Title, i.Description, i.Severity, i.Status, i.AffectedComponents, i.StartedAt, i.ResolvedAt,
        i.Updates.OrderBy(u => u.PostedAt).Select(u => new IncidentUpdateDto(u.Status, u.Message, u.PostedAt)).ToList());
}
