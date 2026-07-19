using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Domain.Tenancy;

public class Department : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public string Name { get; private set; } = default!; // Kitchen, Front of House, Bar, Housekeeping...

    private Department() { }

    public Department(Guid tenantId, Guid locationId, string name)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Name = name;
    }

    public void Rename(string name) => Name = name;
}

public class Employee : TenantAuditableEntity
{
    public Guid DepartmentId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid UserId { get; private set; } // links to ApplicationUser in Infrastructure
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string JobTitle { get; private set; } = default!;
    public EmploymentStatus Status { get; private set; } = EmploymentStatus.Active;
    public DateOnly HireDate { get; private set; }
    public decimal? HourlyRate { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private Employee() { }

    public Employee(Guid tenantId, Guid departmentId, Guid locationId, Guid userId,
        string firstName, string lastName, string jobTitle, DateOnly hireDate, decimal? hourlyRate)
    {
        TenantId = tenantId;
        DepartmentId = departmentId;
        LocationId = locationId;
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        JobTitle = jobTitle;
        HireDate = hireDate;
        HourlyRate = hourlyRate;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Terminate() => Status = EmploymentStatus.Terminated;
    public void PutOnLeave() => Status = EmploymentStatus.OnLeave;
    public void Reactivate() => Status = EmploymentStatus.Active;
    public void Transfer(Guid departmentId, Guid locationId)
    {
        DepartmentId = departmentId;
        LocationId = locationId;
    }
}
