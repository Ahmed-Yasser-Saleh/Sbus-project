using SBus.Domain.Accounts;

namespace SBus.Application.Features.MobileAuth;

public interface IMobileRefreshTokenStore
{
    Task AddAsync(MobileRefreshToken token, CancellationToken ct);
    Task<MobileRefreshToken?> FindAsync(string tokenHash, CancellationToken ct);
    Task<bool> RotateAsync(MobileRefreshToken previous, MobileRefreshToken replacement, DateTimeOffset now, CancellationToken ct);
    Task RevokeFamilyAsync(string userId, Guid familyId, DateTimeOffset now, CancellationToken ct);
    Task<bool> IsFamilyActiveAsync(string userId, Guid familyId, DateTimeOffset now, CancellationToken ct);
}