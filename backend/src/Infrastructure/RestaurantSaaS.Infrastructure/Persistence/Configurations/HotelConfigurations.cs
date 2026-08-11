using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Hotel;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("hotel_rooms");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.LocationId, x.Number }).IsUnique();
        builder.Property(x => x.Number).HasMaxLength(20).IsRequired();
        builder.Property(x => x.NightlyRate).HasColumnType("numeric(10,2)");
    }
}

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("hotel_guests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
    }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("hotel_reservations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RoomId);
        builder.HasIndex(x => x.GuestId);
    }
}

public class GuestStayConfiguration : IEntityTypeConfiguration<GuestStay>
{
    public void Configure(EntityTypeBuilder<GuestStay> builder)
    {
        builder.ToTable("hotel_guest_stays");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ReservationId);
        builder.Property(x => x.RoomChargeBalance).HasColumnType("numeric(10,2)");

        builder.HasMany(x => x.RoomServiceOrders).WithOne().HasForeignKey(r => r.GuestStayId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.MinibarCharges).WithOne().HasForeignKey(m => m.GuestStayId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoomServiceOrderConfiguration : IEntityTypeConfiguration<RoomServiceOrder>
{
    public void Configure(EntityTypeBuilder<RoomServiceOrder> builder)
    {
        builder.ToTable("hotel_room_service_orders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuestStayId);
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
    }
}

public class MinibarChargeConfiguration : IEntityTypeConfiguration<MinibarCharge>
{
    public void Configure(EntityTypeBuilder<MinibarCharge> builder)
    {
        builder.ToTable("hotel_minibar_charges");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuestStayId);
        builder.Property(x => x.ItemName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
    }
}
