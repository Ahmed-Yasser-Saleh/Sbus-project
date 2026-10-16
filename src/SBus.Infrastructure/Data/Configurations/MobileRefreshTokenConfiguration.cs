using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SBus.Domain.Accounts;
using SBus.Infrastructure.Identity;

namespace SBus.Infrastructure.Data.Configurations;

public sealed class MobileRefreshTokenConfiguration : IEntityTypeConfiguration<MobileRefreshToken>
{
    public void Configure(EntityTypeBuilder<MobileRefreshToken> builder)
    {
        builder.ToTable("MobileRefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SecurityStampHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.FamilyId });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MobileRefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
    }
}