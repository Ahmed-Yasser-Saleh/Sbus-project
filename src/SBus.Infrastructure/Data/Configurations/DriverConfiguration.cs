using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Common.Constants;
using SBus.Domain.Drivers;

namespace SBus.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(SBusConstants.NameMaxLength).IsRequired();
        builder.Property(d => d.PhoneNumber).HasMaxLength(SBusConstants.PhoneMaxLength).IsRequired();
    }
}
