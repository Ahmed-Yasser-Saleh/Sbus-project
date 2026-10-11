using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Common.Constants;
using SBus.Infrastructure.Identity;

namespace SBus.Infrastructure.Data.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Nullable for existing users and accounts initially created through Google.
        builder.Property(u => u.FullName).HasMaxLength(SBusConstants.NameMaxLength);
    }
}
