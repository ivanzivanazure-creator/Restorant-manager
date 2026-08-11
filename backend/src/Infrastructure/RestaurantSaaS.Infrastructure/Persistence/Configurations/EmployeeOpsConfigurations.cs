using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Employees;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("employee_shifts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.EmployeeId, x.StartsAt });
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("employee_attendance_records");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EmployeeId);
        builder.Ignore(x => x.WorkedTime);
    }
}

public class PayrollEntryConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> builder)
    {
        builder.ToTable("employee_payroll_entries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EmployeeId);
        builder.Property(x => x.BaseAmount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.BonusTotal).HasColumnType("numeric(10,2)");
        builder.Property(x => x.DeductionsTotal).HasColumnType("numeric(10,2)");
        builder.Ignore(x => x.NetAmount);
        builder.HasMany(x => x.Bonuses).WithOne().HasForeignKey(b => b.PayrollEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("employee_bonuses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PayrollEntryId);
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.Reason).HasMaxLength(200);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("employee_leave_requests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EmployeeId);
        builder.Property(x => x.ReviewerNote).HasMaxLength(500);
    }
}

public class PerformanceReviewConfiguration : IEntityTypeConfiguration<PerformanceReview>
{
    public void Configure(EntityTypeBuilder<PerformanceReview> builder)
    {
        builder.ToTable("employee_performance_reviews");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EmployeeId);
        builder.Property(x => x.Summary).HasMaxLength(2000);
    }
}
