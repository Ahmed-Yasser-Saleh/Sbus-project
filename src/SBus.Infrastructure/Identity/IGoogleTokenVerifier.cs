using Google.Apis.Auth;

namespace SBus.Infrastructure.Identity;

public interface IGoogleTokenVerifier
{
    Task<GoogleJsonWebSignature.Payload> VerifyAsync(string idToken, IReadOnlyList<string> audiences, CancellationToken ct);
}