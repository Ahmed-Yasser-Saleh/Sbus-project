using Google.Apis.Auth;

namespace SBus.Infrastructure.Identity;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    public Task<GoogleJsonWebSignature.Payload> VerifyAsync(string idToken, IReadOnlyList<string> audiences, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = audiences }).WaitAsync(ct);
    }
}