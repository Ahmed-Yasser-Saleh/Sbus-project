using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Routes;

namespace SBus.Infrastructure.Data.Configurations;

public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(r => r.Stop)
            .WithMany()
            .HasForeignKey(r => r.StopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.Direction, r.StopId }).IsUnique();
        builder.HasIndex(r => new { r.Direction, r.Order }).IsUnique();
    }
}
