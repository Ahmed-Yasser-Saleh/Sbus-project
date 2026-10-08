using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Bookings;
using SBus.Domain.Common.Constants;
using SBus.Domain.Stops;

namespace SBus.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.PublicToken).HasMaxLength(64).IsRequired();
        builder.HasIndex(b => b.PublicToken).IsUnique();

        builder.Property(b => b.PassengerName).HasMaxLength(SBusConstants.NameMaxLength).IsRequired();
        builder.Property(b => b.PhoneNumber).HasMaxLength(SBusConstants.PhoneMaxLength).IsRequired();
        builder.Property(b => b.PricePerSeat).HasPrecision(10, 2);
        builder.Property(b => b.Amount).HasPrecision(10, 2);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(b => b.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.CancelledBy).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.ReceiptFileName).HasMaxLength(100);
        builder.Property(b => b.RejectionReason).HasMaxLength(Booking.RejectionReasonMaxLength);

        builder.Ignore(b => b.SeatNumbers);
        builder.Ignore(b => b.HoldsSeats);

        builder.HasOne(b => b.Trip).WithMany().HasForeignKey(b => b.TripId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Stop>().WithMany().HasForeignKey(b => b.PickupStopId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Stop>().WithMany().HasForeignKey(b => b.DropoffStopId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Seats)
            .WithOne()
            .HasForeignKey(s => s.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(b => new { b.TripId, b.Status });
        builder.HasIndex(b => new { b.Status, b.HoldExpiresAtUtc });
    }
}
