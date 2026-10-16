namespace SBus.Domain.Accounts;

public sealed class MobileRefreshToken
{
    private MobileRefreshToken() { }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public string SecurityStampHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public long Version { get; private set; }

    public static MobileRefreshToken Create(string userId, Guid familyId, string tokenHash, string stampHash, DateTimeOffset now, DateTimeOffset expires)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (familyId == Guid.Empty || tokenHash.Length != 64 || stampHash.Length != 64 || expires <= now)
        {
            throw new ArgumentException("Invalid refresh session.");
        }

        return new MobileRefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            SecurityStampHash = stampHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = expires,
        };
    }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public void Revoke(DateTimeOffset now, Guid? replacement = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = now;
        ReplacedByTokenId = replacement;
        Version++;
    }
}