using Google.Apis.Auth;

using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;

namespace SBus.Infrastructure.Identity;

public sealed class GoogleIdentityValidator(IOptions<MobileAuthOptions> options, IGoogleTokenVerifier verifier) : IGoogleIdentityValidator
{
    public async Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (options.Value.GoogleClientIds.Length == 0)
        {
            throw new ServiceUnavailableException("Google sign-in is unavailable.", ErrorCodes.GoogleUnavailable);
        }

        try
        {
            var payload = await verifier.VerifyAsync(idToken, options.Value.GoogleClientIds, ct);
            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
            {
                throw InvalidToken();
            }

            return new(payload.Subject, payload.Email, payload.EmailVerified, payload.HostedDomain, payload.Name);
        }
        catch (Exception ex) when (ex is InvalidJwtException or FormatException or ArgumentException)
        {
            throw InvalidToken();
        }
    }

    private static UnauthenticatedException InvalidToken() => new("Unable to validate Google sign-in.", ErrorCodes.GoogleTokenInvalid);
}