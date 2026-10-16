using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;

namespace SBus.Infrastructure.Identity;

public sealed class GoogleLinkProof(IDataProtectionProvider provider) : IGoogleLinkProof
{
    private readonly ITimeLimitedDataProtector _protector = provider.CreateProtector("SBus.Mobile.GoogleLink.v1").ToTimeLimitedDataProtector();
    public string Protect(GoogleIdentity identity) => _protector.Protect(JsonSerializer.Serialize(identity), TimeSpan.FromMinutes(5));

    public GoogleIdentity Unprotect(string proof)
    {
        try
        {
            return JsonSerializer.Deserialize<GoogleIdentity>(_protector.Unprotect(proof))
                ?? throw new JsonException();
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException or ArgumentException)
        {
            throw new UnauthenticatedException("Restart Google sign-in to link your account.", ErrorCodes.GoogleLinkInvalid);
        }
    }
}