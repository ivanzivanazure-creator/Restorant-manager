using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Status;

/// <summary>A platform-wide incident/maintenance note shown on the public status page. Not tenant-scoped
/// — this describes the platform itself, managed by SuperAdmin, and backs the SLA commitment shown per
/// Package.SlaTier (see docs/ARCHITECTURE.md "Status page & SLA").</summary>
public class SystemIncident : AuditableEntity
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; } = IncidentStatus.Investigating;
    public IReadOnlyCollection<PlatformComponent> AffectedComponents { get; private set; } = [];
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private readonly List<IncidentUpdate> _updates = [];
    public IReadOnlyCollection<IncidentUpdate> Updates => _updates.AsReadOnly();

    private SystemIncident() { }

    public SystemIncident(string title, string description, IncidentSeverity severity, IReadOnlyCollection<PlatformComponent> affectedComponents)
    {
        Title = title;
        Description = description;
        Severity = severity;
        AffectedComponents = affectedComponents;
        StartedAt = DateTimeOffset.UtcNow;
        CreatedAt = StartedAt;
        _updates.Add(new IncidentUpdate(Id, IncidentStatus.Investigating, description));
    }

    public void PostUpdate(IncidentStatus status, string message)
    {
        if (Status == IncidentStatus.Resolved) throw new DomainException("Cannot update a resolved incident; open a new one instead.");
        Status = status;
        _updates.Add(new IncidentUpdate(Id, status, message));
        if (status == IncidentStatus.Resolved) ResolvedAt = DateTimeOffset.UtcNow;
    }
}

public class IncidentUpdate : BaseEntity
{
    public Guid IncidentId { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string Message { get; private set; } = default!;
    public DateTimeOffset PostedAt { get; private set; }

    private IncidentUpdate() { }

    internal IncidentUpdate(Guid incidentId, IncidentStatus status, string message)
    {
        IncidentId = incidentId;
        Status = status;
        Message = message;
        PostedAt = DateTimeOffset.UtcNow;
    }
}
