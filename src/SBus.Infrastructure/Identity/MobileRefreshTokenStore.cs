using Microsoft.EntityFrameworkCore;

using SBus.Application.Features.MobileAuth;
using SBus.Domain.Accounts;
using SBus.Infrastructure.Data;

namespace SBus.Infrastructure.Identity;

public sealed class MobileRefreshTokenStore(AppDbContext db) : IMobileRefreshTokenStore
{
    public async Task AddAsync(MobileRefreshToken token, CancellationToken ct)
    {
        db.Set<MobileRefreshToken>().Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<MobileRefreshToken?> FindAsync(string tokenHash, CancellationToken ct) =>
        db.Set<MobileRefreshToken>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public async Task<bool> RotateAsync(MobileRefreshToken previous, MobileRefreshToken replacement, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockFamilyAsync(previous.FamilyId, ct);
        var current = await db.Set<MobileRefreshToken>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == previous.Id, ct);
        if (current is null || current.Version != previous.Version || !current.IsActive(now))
        {
            return false;
        }

        db.Set<MobileRefreshToken>().Add(replacement);
        await db.SaveChangesAsync(ct);
        var updated = await db.Set<MobileRefreshToken>()
            .Where(x => x.Id == previous.Id && x.Version == previous.Version && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAtUtc, now)
                    .SetProperty(x => x.ReplacedByTokenId, replacement.Id).SetProperty(x => x.Version, x => x.Version + 1), ct);
        if (updated != 1)
        {
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task RevokeFamilyAsync(string userId, Guid familyId, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockFamilyAsync(familyId, ct);
        await db.Set<MobileRefreshToken>().Where(x => x.UserId == userId && x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now).SetProperty(x => x.Version, x => x.Version + 1), ct);
        await transaction.CommitAsync(ct);
    }

    public Task<bool> IsFamilyActiveAsync(string userId, Guid familyId, DateTimeOffset now, CancellationToken ct) =>
        db.Set<MobileRefreshToken>().AnyAsync(x => x.UserId == userId && x.FamilyId == familyId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now, ct);

    private Task LockFamilyAsync(Guid familyId, CancellationToken ct)
    {
        // Serialize rotation and logout for a family, including rows created during rotation.
        var key = BitConverter.ToInt64(familyId.ToByteArray());
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", ct);
    }
}