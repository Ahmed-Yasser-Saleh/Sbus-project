namespace SBus.Application.Features.MobileAuth;

public interface IMobileAuthService
{
    Task<MobileTokens> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<MobileTokens> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<MobileGoogleResult> GoogleAsync(GoogleRequest request, CancellationToken ct);
    Task<MobileTokens> LinkGoogleAsync(GoogleLinkRequest request, CancellationToken ct);
    Task LogoutAsync(string userId, Guid familyId, CancellationToken ct);
    Task<MobileAccount?> ValidateSessionAsync(string userId, string stampHash, Guid familyId, CancellationToken ct);
}