using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Validation;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Domain.Common.Results;

namespace SBus.Infrastructure.Identity;

public sealed class TravelerAccounts(UserManager<AppUser> users, SignInManager<AppUser> signIn, IIdentityTransaction transaction)
    : ITravelerRegistration
{
    public const string VerifiedEmailClaim = "google:email_verified";
    public const string HostedDomainClaim = "google:hd";

    async Task<Result<TravelerRegistrationOutcome>> ITravelerRegistration.RegisterAsync(string email, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var candidate = new AppUser { UserName = email.Trim(), Email = email.Trim() };
        foreach (var validator in users.PasswordValidators)
        {
            // Validate password policy before any email lookup, including duplicates.
            if (!(await validator.ValidateAsync(users, candidate, password)).Succeeded)
            {
                return Error.Validation(
                    nameof(RegisterTravelerCommand.Password),
                    "Password does not meet the configured security requirements.");
            }
        }

        ct.ThrowIfCancellationRequested();
        var user = await RegisterAsync(email, password);
        if (user is null && await users.FindByEmailAsync(candidate.Email!) is not null)
        {
            return Error.Validation(
                nameof(RegisterTravelerCommand.Email),
                "البريد الإلكتروني مستخدم بالفعل.");
        }

        return new TravelerRegistrationOutcome(user?.Id);
    }

    public async Task<AppUser?> RegisterAsync(string email, string password)
    {
        email = email.Trim();
        if (!PublicEmailAddress.IsValid(email))
        {
            return null;
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            return null;
        }

        try
        {
            return await transaction.ExecuteAsync(
                async () =>
            {
                var user = new AppUser { UserName = email, Email = email };
                if (!(await users.CreateAsync(user, password)).Succeeded)
                {
                    return null;
                }

                return (await users.AddToRoleAsync(user, Roles.Traveler)).Succeeded ? user : null;
            }, user => user is not null);
        }
        catch (UniqueConstraintException)
        {
            // Concurrent requests for the same normalized email must not create duplicates.
            return null;
        }
    }

    public static bool HasVerifiedGoogleEmail(ExternalLoginInfo info) =>
        info.LoginProvider == "Google"
        && !string.IsNullOrWhiteSpace(info.ProviderKey)
        && bool.TryParse(info.Principal.FindFirstValue(VerifiedEmailClaim), out var verified)
        && verified
        && !string.IsNullOrWhiteSpace(info.Principal.FindFirstValue(ClaimTypes.Email));

    public static bool GoogleIsEmailAuthority(ExternalLoginInfo info) =>
        HasVerifiedGoogleEmail(info)
        && (info.Principal.FindFirstValue(ClaimTypes.Email)!.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(info.Principal.FindFirstValue(HostedDomainClaim)));

    public async Task<ExternalAccountResult> ResolveGoogleAsync(ExternalLoginInfo info)
    {
        if (!HasVerifiedGoogleEmail(info))
        {
            return new(ExternalAccountState.Rejected);
        }

        var linked = await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (linked is not null)
        {
            return new(ExternalAccountState.Ready, linked);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)!.Trim();
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Linking is restricted to Traveler-only accounts. Company/Office accounts
            // can only use an external login explicitly provisioned by an administrator.
            var roles = await users.GetRolesAsync(existing);
            return roles.Count == 1 && roles[0] == Roles.Traveler
                && existing.EmailConfirmed && await users.HasPasswordAsync(existing)
                ? new(ExternalAccountState.PasswordProofRequired)
                : new(ExternalAccountState.Rejected);
        }

        try
        {
            return await transaction.ExecuteAsync(
                async () =>
            {
                var user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = GoogleIsEmailAuthority(info),
                };
                if (!(await users.CreateAsync(user)).Succeeded
                    || !(await users.AddToRoleAsync(user, Roles.Traveler)).Succeeded
                    || !(await users.AddLoginAsync(user, info)).Succeeded)
                {
                    return new ExternalAccountResult(ExternalAccountState.Rejected);
                }

                return new ExternalAccountResult(ExternalAccountState.Ready, user);
            }, result => result.State == ExternalAccountState.Ready);
        }
        catch (UniqueConstraintException)
        {
            return new(ExternalAccountState.Rejected);
        }
    }

    public async Task<AppUser?> LinkGoogleWithPasswordAsync(ExternalLoginInfo info, string password)
    {
        if (!HasVerifiedGoogleEmail(info))
        {
            return null;
        }

        var user = await users.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email)!);
        if (user is null || !user.EmailConfirmed)
        {
            return null;
        }

        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 1 || roles[0] != Roles.Traveler
            || !(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            return null;
        }

        try
        {
            return (await users.AddLoginAsync(user, info)).Succeeded ? user : null;
        }
        catch (UniqueConstraintException)
        {
            return null;
        }
    }
}
