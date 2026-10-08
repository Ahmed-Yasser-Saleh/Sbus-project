using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Application.Common.Persistence;
using SBus.Domain.Bookings;
using SBus.Domain.Trips;

namespace SBus.Infrastructure.Data.Configurations;

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne<Trip>().WithMany().HasForeignKey(s => s.TripId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.TripId, s.SeatNumber })
            .IsUnique()
            .HasFilter("\"IsActive\"")
            .HasDatabaseName(DbConstraintNames.ActiveSeatPerTrip);
    }
}
