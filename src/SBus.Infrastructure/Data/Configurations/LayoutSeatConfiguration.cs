using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Fleet;

namespace SBus.Infrastructure.Data.Configurations;

public class LayoutSeatConfiguration : IEntityTypeConfiguration<LayoutSeat>
{
    public void Configure(EntityTypeBuilder<LayoutSeat> builder)
    {
        builder.ToTable("LayoutSeats");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.SeatLayoutId, s.SeatNumber }).IsUnique();
    }
}
