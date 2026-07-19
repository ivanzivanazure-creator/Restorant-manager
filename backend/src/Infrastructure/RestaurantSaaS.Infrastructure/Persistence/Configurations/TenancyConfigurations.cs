using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Tenancy;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class RestaurantOwnerConfiguration : IEntityTypeConfiguration<RestaurantOwner>
{
    public void Configure(EntityTypeBuilder<RestaurantOwner> builder)
    {
        builder.ToTable("restaurant_owners");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.ContactEmail);
        builder.HasMany(x => x.Restaurants).WithOne().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.HasMany(x => x.Locations).WithOne().HasForeignKey(l => l.RestaurantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RestaurantId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(p => p.Line1).HasColumnName("address_line1").HasMaxLength(300);
            a.Property(p => p.Line2).HasColumnName("address_line2").HasMaxLength(300);
            a.Property(p => p.City).HasColumnName("address_city").HasMaxLength(150);
            a.Property(p => p.State).HasColumnName("address_state").HasMaxLength(150);
            a.Property(p => p.PostalCode).HasColumnName("address_postal_code").HasMaxLength(20);
            a.Property(p => p.Country).HasColumnName("address_country").HasMaxLength(100);
        });

        builder.OwnsOne(x => x.Coordinates, c =>
        {
            c.Property(p => p.Latitude).HasColumnName("latitude");
            c.Property(p => p.Longitude).HasColumnName("longitude");
        });

        builder.Property(x => x.SupportedLanguages)
            .HasConversion(JsonValueConverter<IReadOnlyCollection<string>>.Converter, JsonValueConverter<IReadOnlyCollection<string>>.Comparer)
            .HasColumnType("jsonb");

        builder.HasMany(x => x.WorkingHours).WithOne().HasForeignKey(w => w.LocationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.TaxConfig).WithOne().HasForeignKey<TaxConfig>(t => t.LocationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkingHourConfiguration : IEntityTypeConfiguration<WorkingHour>
{
    public void Configure(EntityTypeBuilder<WorkingHour> builder)
    {
        builder.ToTable("working_hours");
        builder.HasKey(x => x.Id);
        builder.OwnsOne(x => x.Hours, h =>
        {
            h.Property(p => p.Start).HasColumnName("start_time");
            h.Property(p => p.End).HasColumnName("end_time");
        });
    }
}

public class TaxConfigConfiguration : IEntityTypeConfiguration<TaxConfig>
{
    public void Configure(EntityTypeBuilder<TaxConfig> builder)
    {
        builder.ToTable("tax_configs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaxLabel).HasMaxLength(50);
        builder.Property(x => x.DefaultTaxRatePercent).HasColumnType("numeric(5,2)");
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.LocationId);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.UserId);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.JobTitle).HasMaxLength(100).IsRequired();
        builder.Property(x => x.HourlyRate).HasColumnType("numeric(10,2)");
    }
}
