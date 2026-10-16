namespace SBus.Application.Features.MobileAuth;

public interface IGoogleIdentityValidator
{
    Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken ct);
}