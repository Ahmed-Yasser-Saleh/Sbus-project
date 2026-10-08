using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Common.Constants;
using SBus.Domain.Fleet;

namespace SBus.Infrastructure.Data.Configurations;

public class SeatLayoutConfiguration : IEntityTypeConfiguration<SeatLayout>
{
    public void Configure(EntityTypeBuilder<SeatLayout> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(SBusConstants.NameMaxLength).IsRequired();
        builder.HasIndex(l => l.Name).IsUnique();

        builder.Ignore(l => l.Capacity);

        builder.HasMany(l => l.Seats)
            .WithOne()
            .HasForeignKey(s => s.SeatLayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
