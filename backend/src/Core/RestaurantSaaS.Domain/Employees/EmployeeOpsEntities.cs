using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Employees;

public class Shift : TenantAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public ShiftStatus Status { get; private set; } = ShiftStatus.Scheduled;

    private Shift() { }

    public Shift(Guid tenantId, Guid employeeId, Guid departmentId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt) throw new DomainException("Shift end must be after start.");
        TenantId = tenantId;
        EmployeeId = employeeId;
        DepartmentId = departmentId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void Start() => Status = ShiftStatus.InProgress;
    public void Complete() => Status = ShiftStatus.Completed;
    public void MarkMissed() => Status = ShiftStatus.Missed;
}

public class AttendanceRecord : TenantAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Guid? ShiftId { get; private set; }
    public DateTimeOffset ClockIn { get; private set; }
    public DateTimeOffset? ClockOut { get; private set; }

    public TimeSpan? WorkedTime => ClockOut is null ? null : ClockOut - ClockIn;

    private AttendanceRecord() { }

    public AttendanceRecord(Guid tenantId, Guid employeeId, Guid? shiftId)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ShiftId = shiftId;
        ClockIn = DateTimeOffset.UtcNow;
    }

    public void RecordClockOut() => ClockOut = DateTimeOffset.UtcNow;
}

public class PayrollEntry : TenantAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal BaseAmount { get; private set; }
    public decimal BonusTotal { get; private set; }
    public decimal DeductionsTotal { get; private set; }
    public bool IsPaid { get; private set; }

    private readonly List<Bonus> _bonuses = [];
    public IReadOnlyCollection<Bonus> Bonuses => _bonuses.AsReadOnly();

    public decimal NetAmount => BaseAmount + BonusTotal - DeductionsTotal;

    private PayrollEntry() { }

    public PayrollEntry(Guid tenantId, Guid employeeId, DateOnly periodStart, DateOnly periodEnd, decimal baseAmount)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        BaseAmount = baseAmount;
    }

    public Bonus AddBonus(decimal amount, string reason)
    {
        var bonus = new Bonus(Id, amount, reason);
        _bonuses.Add(bonus);
        BonusTotal += amount;
        return bonus;
    }

    public void ApplyDeduction(decimal amount) => DeductionsTotal += amount;
    public void MarkPaid() => IsPaid = true;
}

public class Bonus : BaseEntity
{
    public Guid PayrollEntryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = default!;

    private Bonus() { }

    internal Bonus(Guid payrollEntryId, decimal amount, string reason)
    {
        PayrollEntryId = payrollEntryId;
        Amount = amount;
        Reason = reason;
    }
}

public class LeaveRequest : TenantAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public LeaveType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Pending;
    public string? ReviewerNote { get; private set; }

    private LeaveRequest() { }

    public LeaveRequest(Guid tenantId, Guid employeeId, LeaveType type, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate) throw new DomainException("Leave end date must not precede start date.");
        TenantId = tenantId;
        EmployeeId = employeeId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Approve(string? note = null)
    {
        Status = LeaveRequestStatus.Approved;
        ReviewerNote = note;
    }

    public void Reject(string? note = null)
    {
        Status = LeaveRequestStatus.Rejected;
        ReviewerNote = note;
    }

    public void Cancel() => Status = LeaveRequestStatus.Cancelled;
}

public class PerformanceReview : TenantAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Guid ReviewedByEmployeeId { get; private set; }
    public int Score { get; private set; } // 1-5
    public string Summary { get; private set; } = default!;
    public DateOnly ReviewDate { get; private set; }

    private PerformanceReview() { }

    public PerformanceReview(Guid tenantId, Guid employeeId, Guid reviewedByEmployeeId, int score, string summary, DateOnly reviewDate)
    {
        if (score is < 1 or > 5) throw new DomainException("Score must be between 1 and 5.");
        TenantId = tenantId;
        EmployeeId = employeeId;
        ReviewedByEmployeeId = reviewedByEmployeeId;
        Score = score;
        Summary = summary;
        ReviewDate = reviewDate;
    }
}
