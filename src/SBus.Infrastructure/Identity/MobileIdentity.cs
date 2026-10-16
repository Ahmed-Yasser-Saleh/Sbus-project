using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.MobileAuth;

namespace SBus.Infrastructure.Identity;

public sealed class MobileIdentity(UserManager<AppUser> users, SignInManager<AppUser> signIn,
    TravelerAccounts travelers, IRegistrationConfirmationSender confirmation) : IMobileIdentity
{
    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString());

    public async Task<MobileAccount> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            _ = users.PasswordHasher.VerifyHashedPassword(new AppUser(), DummyHash, password);
            throw InvalidCredentials();
        }

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.Succeeded && await IsAllowedAsync(user))
        {
            return await MapAsync(user);
        }

        if (result.IsNotAllowed && signIn.Options.SignIn.RequireConfirmedEmail && !user.EmailConfirmed
            && (!users.SupportsUserLockout || !await users.IsLockedOutAsync(user)))
        {
            if (await users.CheckPasswordAsync(user, password))
            {
                throw new ForbiddenException("Confirm your email before signing in.", ErrorCodes.EmailConfirmationRequired);
            }

            if (users.SupportsUserLockout && await users.GetLockoutEnabledAsync(user))
            {
                _ = await users.AccessFailedAsync(user);
            }
        }

        throw InvalidCredentials();
    }

    public async Task<MobileAccount?> FindActiveAsync(string userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        return user is not null && await IsAllowedAsync(user) ? await MapAsync(user) : null;
    }

    public async Task<MobileGoogleAccount> ResolveGoogleAsync(GoogleIdentity identity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = await travelers.ResolveGoogleAsync(ToExternalLogin(identity));
        if (result.State == ExternalAccountState.PasswordProofRequired)
        {
            return new(MobileGoogleState.PasswordProofRequired, null);
        }

        if (result.State != ExternalAccountState.Ready || result.User is null)
        {
            throw new UnauthenticatedException("Unable to sign in with Google.", ErrorCodes.GoogleSignInFailed);
        }

        if (!result.User.EmailConfirmed)
        {
            await confirmation.SendAsync(result.User.Id, ct);
            return new(MobileGoogleState.EmailConfirmationRequired, null);
        }

        if (!await IsAllowedAsync(result.User))
        {
            throw InvalidCredentials();
        }

        return new(MobileGoogleState.Ready, await MapAsync(result.User));
    }

    public async Task<MobileAccount> LinkGoogleAsync(GoogleIdentity identity, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await travelers.LinkGoogleWithPasswordAsync(ToExternalLogin(identity), password);
        if (user is null || !await IsAllowedAsync(user))
        {
            throw new UnauthenticatedException("Unable to link this Google account.", ErrorCodes.GoogleLinkFailed);
        }

        return await MapAsync(user);
    }

    private async Task<bool> IsAllowedAsync(AppUser user) => await signIn.CanSignInAsync(user)
        && (!users.SupportsUserLockout || !await users.IsLockedOutAsync(user))
        && (!await users.GetTwoFactorEnabledAsync(user));

    private async Task<MobileAccount> MapAsync(AppUser user) => new(user.Id, [.. await users.GetRolesAsync(user)],
        MobileAuthService.Hash(await users.GetSecurityStampAsync(user)), new TravelerProfileDto(user.FullName, user.PhoneNumber).IsComplete);

    private static ExternalLoginInfo ToExternalLogin(GoogleIdentity identity)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identity.Subject), new(ClaimTypes.Email, identity.Email),
            new(TravelerAccounts.VerifiedEmailClaim, identity.EmailVerified.ToString()),
        };
        if (identity.HostedDomain is not null)
        {
            claims.Add(new(TravelerAccounts.HostedDomainClaim, identity.HostedDomain));
        }

        if (identity.Name is not null)
        {
            claims.Add(new(ClaimTypes.Name, identity.Name));
        }

        return new(new ClaimsPrincipal(new ClaimsIdentity(claims, "Google")), "Google", identity.Subject, "Google");
    }

    private static UnauthenticatedException InvalidCredentials() => new("Unable to sign in. Check your credentials or try again later.", ErrorCodes.InvalidCredentials);
}