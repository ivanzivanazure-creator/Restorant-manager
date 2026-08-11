using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add`/`database update` construct the DbContext directly — with a
/// design-time-only connection string and a no-op tenant provider — instead of booting the full API host
/// (which would otherwise run Program.cs's startup EnsureCreated/seed logic against a real database).</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=restaurant_saas;Username=restaurant_saas;Password=design-time-only");

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid? TenantId => null;
        public Guid? LocationId => null;
        public bool IsSuperAdmin => true;
    }
}
