using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Common.Constants;
using SBus.Domain.Fleet;

namespace SBus.Infrastructure.Data.Configurations;

public class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.PlateNumber).HasMaxLength(SBusConstants.NameMaxLength).IsRequired();
        builder.HasIndex(b => b.PlateNumber).IsUnique();

        builder.HasOne(b => b.SeatLayout)
            .WithMany()
            .HasForeignKey(b => b.SeatLayoutId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
