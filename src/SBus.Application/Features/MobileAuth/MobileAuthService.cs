using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Domain.Accounts;

namespace SBus.Application.Features.MobileAuth;

public sealed class MobileAuthService(
    IMobileIdentity identity, IMobileRefreshTokenStore store, IAccessTokenGenerator accessTokens,
    IGoogleIdentityValidator google, IGoogleLinkProof linking, TimeProvider clock, IOptions<MobileAuthOptions> options) : IMobileAuthService
{
    public async Task<MobileTokens> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await new LoginRequestValidator().ValidateAndThrowAsync(request, ct);
        return await IssueAsync(await identity.AuthenticateAsync(request.Email.Trim(), request.Password, ct), ct);
    }

    public async Task<MobileTokens> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        await new RefreshRequestValidator().ValidateAndThrowAsync(request, ct);
        var previous = await store.FindAsync(Hash(request.RefreshToken), ct);
        var now = clock.GetUtcNow();
        if (previous is null)
        {
            throw InvalidRefresh();
        }

        var account = await identity.FindActiveAsync(previous.UserId, ct);
        if (!previous.IsActive(now) || account is null || !FixedEquals(previous.SecurityStampHash, account.SecurityStampHash))
        {
            await store.RevokeFamilyAsync(previous.UserId, previous.FamilyId, now, ct);
            throw InvalidRefresh();
        }

        var raw = NewRefreshToken();

        // Rotation retains the family's absolute expiry; repeated refreshes cannot extend it forever.
        var replacement = MobileRefreshToken.Create(account.Id, previous.FamilyId, Hash(raw), account.SecurityStampHash, now, previous.ExpiresAtUtc);
        if (!await store.RotateAsync(previous, replacement, now, ct))
        {
            await store.RevokeFamilyAsync(previous.UserId, previous.FamilyId, now, ct);
            throw InvalidRefresh();
        }

        return Tokens(account, replacement, raw, now);
    }

    public async Task<MobileGoogleResult> GoogleAsync(GoogleRequest request, CancellationToken ct)
    {
        await new GoogleRequestValidator().ValidateAndThrowAsync(request, ct);
        var verified = await google.ValidateAsync(request.IdToken, ct);
        var result = await identity.ResolveGoogleAsync(verified, ct);
        return result.State switch
        {
            MobileGoogleState.Ready when result.Account is not null => new(result.State, await IssueAsync(result.Account, ct), null),
            MobileGoogleState.PasswordProofRequired => new(result.State, null, linking.Protect(verified)),
            MobileGoogleState.EmailConfirmationRequired => new(result.State, null, null),
            _ => throw new UnauthenticatedException("Unable to sign in with Google.", ErrorCodes.GoogleSignInFailed),
        };
    }

    public async Task<MobileTokens> LinkGoogleAsync(GoogleLinkRequest request, CancellationToken ct)
    {
        await new GoogleLinkRequestValidator().ValidateAndThrowAsync(request, ct);
        return await IssueAsync(await identity.LinkGoogleAsync(linking.Unprotect(request.LinkingProof), request.Password, ct), ct);
    }

    public Task LogoutAsync(string userId, Guid familyId, CancellationToken ct) => store.RevokeFamilyAsync(userId, familyId, clock.GetUtcNow(), ct);

    public async Task<MobileAccount?> ValidateSessionAsync(string userId, string stampHash, Guid familyId, CancellationToken ct)
    {
        var account = await identity.FindActiveAsync(userId, ct);
        return account is not null && FixedEquals(account.SecurityStampHash, stampHash)
            && await store.IsFamilyActiveAsync(userId, familyId, clock.GetUtcNow(), ct) ? account : null;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private async Task<MobileTokens> IssueAsync(MobileAccount account, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var raw = NewRefreshToken();
        var token = MobileRefreshToken.Create(account.Id, Guid.CreateVersion7(), Hash(raw), account.SecurityStampHash, now, now.AddDays(options.Value.RefreshTokenDays));
        await store.AddAsync(token, ct);
        return Tokens(account, token, raw, now);
    }

    private MobileTokens Tokens(MobileAccount account, MobileRefreshToken token, string raw, DateTimeOffset now)
    {
        var expires = now.AddMinutes(options.Value.AccessTokenMinutes);
        return new(accessTokens.Generate(account, token.FamilyId, now, expires), raw, expires, token.ExpiresAtUtc, account);
    }

    private static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private static UnauthenticatedException InvalidRefresh() => new("Your session has expired. Sign in again.", ErrorCodes.RefreshInvalid);
    private static bool FixedEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}