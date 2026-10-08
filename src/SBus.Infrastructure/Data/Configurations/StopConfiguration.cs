using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Common.Constants;
using SBus.Domain.Stops;

namespace SBus.Infrastructure.Data.Configurations;

public class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(SBusConstants.NameMaxLength).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(300);

        builder.HasIndex(s => s.Name).IsUnique();
    }
}
