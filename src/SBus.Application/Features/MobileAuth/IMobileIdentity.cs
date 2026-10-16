namespace SBus.Application.Features.MobileAuth;

public interface IMobileIdentity
{
    Task<MobileAccount> AuthenticateAsync(string email, string password, CancellationToken ct);
    Task<MobileAccount?> FindActiveAsync(string userId, CancellationToken ct);
    Task<MobileGoogleAccount> ResolveGoogleAsync(GoogleIdentity identity, CancellationToken ct);
    Task<MobileAccount> LinkGoogleAsync(GoogleIdentity identity, string password, CancellationToken ct);
}