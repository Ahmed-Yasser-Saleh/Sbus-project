using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Schedules;

namespace SBus.Infrastructure.Data.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Price).HasPrecision(10, 2);

        builder.HasOne(s => s.Bus).WithMany().HasForeignKey(s => s.BusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Driver).WithMany().HasForeignKey(s => s.DriverId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.Direction, s.DepartureTime }).IsUnique();
    }
}
