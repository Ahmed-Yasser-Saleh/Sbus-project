using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Application.Common.Persistence;
using SBus.Domain.Schedules;
using SBus.Domain.Trips;

namespace SBus.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Price).HasPrecision(10, 2);

        builder.HasOne<Schedule>().WithMany().HasForeignKey(t => t.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Bus).WithMany().HasForeignKey(t => t.BusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Driver).WithMany().HasForeignKey(t => t.DriverId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ScheduleId, t.ServiceDate })
            .IsUnique()
            .HasDatabaseName(DbConstraintNames.TripPerScheduleAndDate);

        builder.HasIndex(t => new { t.ServiceDate, t.Direction });
        builder.HasIndex(t => t.DepartureAtUtc);
    }
}
