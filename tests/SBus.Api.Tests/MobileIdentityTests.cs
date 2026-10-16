using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.MobileAuth;
using SBus.Infrastructure.Identity;

using Xunit;

namespace SBus.Api.Tests;

public sealed class MobileIdentityTests
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly IRegistrationConfirmationSender _confirmation = Substitute.For<IRegistrationConfirmationSender>();
    private readonly MobileIdentity _identity;
    private readonly AppUser _user = new() { Id = "user", EmailConfirmed = true, FullName = "Name", PhoneNumber = "+201012345678" };

    public MobileIdentityTests()
    {
        _users = Substitute.For<UserManager<AppUser>>(Substitute.For<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(), Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), Substitute.For<ILogger<UserManager<AppUser>>>());
        _signIn = Substitute.For<SignInManager<AppUser>>(_users, Substitute.For<IHttpContextAccessor>(), Substitute.For<IUserClaimsPrincipalFactory<AppUser>>(),
            Options.Create(new IdentityOptions { SignIn = { RequireConfirmedEmail = true } }), Substitute.For<ILogger<SignInManager<AppUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(), Substitute.For<IUserConfirmation<AppUser>>());
        _identity = new(_users, _signIn, new TravelerAccounts(_users, _signIn, new InlineTransaction()), _confirmation);
        _users.FindByEmailAsync("a@example.com").Returns(_user);
        _users.FindByIdAsync("user").Returns(_user);
        _users.GetRolesAsync(_user).Returns(new List<string> { Roles.Traveler });
        _users.GetSecurityStampAsync(_user).Returns("stamp");
        _signIn.CanSignInAsync(_user).Returns(true);
    }

    [Fact]
    public async Task PasswordLogin_UsesIdentityCheckWithoutIssuingCookie()
    {
        _signIn.CheckPasswordSignInAsync(_user, "password", true).Returns(SignInResult.Success);
        var account = await _identity.AuthenticateAsync("a@example.com", "password", default);
        Assert.Equal(MobileAuthService.Hash("stamp"), account.SecurityStampHash);
        Assert.True(account.ProfileComplete);
        await _signIn.DidNotReceiveWithAnyArgs().PasswordSignInAsync(default(AppUser)!, default!, default, default);
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("locked")]
    [InlineData("unknown")]
    [InlineData("two-factor")]
    public async Task PasswordLogin_IneligibleAccountsUseGenericRejection(string scenario)
    {
        _signIn.CheckPasswordSignInAsync(_user, "password", true).Returns(scenario == "locked" ? SignInResult.LockedOut : SignInResult.Failed);
        if (scenario == "unknown")
        {
            _users.FindByEmailAsync("a@example.com").Returns((AppUser?)null);
        }

        if (scenario == "two-factor")
        {
            _signIn.CheckPasswordSignInAsync(_user, "password", true).Returns(SignInResult.Success);
            _users.GetTwoFactorEnabledAsync(_user).Returns(true);
        }

        var error = await Assert.ThrowsAsync<UnauthenticatedException>(() => _identity.AuthenticateAsync("a@example.com", "password", default));
        Assert.Equal(ErrorCodes.InvalidCredentials, error.ErrorCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmationStatus_IsDisclosedOnlyWithPasswordProof(bool passwordCorrect)
    {
        _user.EmailConfirmed = false;
        _signIn.CheckPasswordSignInAsync(_user, "password", true).Returns(SignInResult.NotAllowed);
        _users.CheckPasswordAsync(_user, "password").Returns(passwordCorrect);
        _users.SupportsUserLockout.Returns(true);
        _users.GetLockoutEnabledAsync(_user).Returns(true);
        if (passwordCorrect)
        {
            var exception = await Assert.ThrowsAsync<ForbiddenException>(() => _identity.AuthenticateAsync("a@example.com", "password", default));
            Assert.Equal(ErrorCodes.EmailConfirmationRequired, exception.ErrorCode);
        }
        else
        {
            await Assert.ThrowsAsync<UnauthenticatedException>(() => _identity.AuthenticateAsync("a@example.com", "password", default));
            await _users.Received().AccessFailedAsync(_user);
        }
    }

    [Fact]
    public async Task ActiveLookup_RechecksStampRolesAndLockout()
    {
        var account = await _identity.FindActiveAsync("user", default);
        Assert.NotNull(account);
        Assert.Contains(Roles.Traveler, account.Roles);
        _users.SupportsUserLockout.Returns(true);
        _users.IsLockedOutAsync(_user).Returns(true);
        Assert.Null(await _identity.FindActiveAsync("user", default));
    }

    [Fact]
    public async Task Google_LinkedSubjectReusesExistingAccount()
    {
        _users.FindByLoginAsync("Google", "subject").Returns(_user);
        var result = await _identity.ResolveGoogleAsync(new("subject", "a@example.com", true, null, "Name"), default);
        Assert.Equal(MobileGoogleState.Ready, result.State);
        Assert.Equal("user", result.Account!.Id);
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(default(AppUser)!);
    }

    [Fact]
    public async Task Google_ThirdPartyEmailRequiresLocalConfirmation()
    {
        _user.EmailConfirmed = false;
        _users.FindByLoginAsync("Google", "subject").Returns(_user);
        var result = await _identity.ResolveGoogleAsync(new("subject", "a@example.com", true, null, "Name"), default);
        Assert.Equal(MobileGoogleState.EmailConfirmationRequired, result.State);
        Assert.Null(result.Account);
        await _confirmation.Received().SendAsync("user", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Google_ExistingPasswordAccountRequiresProof()
    {
        _users.HasPasswordAsync(_user).Returns(true);
        var result = await _identity.ResolveGoogleAsync(new("subject", "a@example.com", true, null, "Name"), default);
        Assert.Equal(MobileGoogleState.PasswordProofRequired, result.State);
    }

    [Fact]
    public async Task GoogleLink_UsesExistingAccountRules()
    {
        _signIn.CheckPasswordSignInAsync(_user, "password", true).Returns(SignInResult.Success);
        _users.AddLoginAsync(_user, Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);
        Assert.Equal("user", (await _identity.LinkGoogleAsync(new("subject", "a@example.com", true, null, "Name"), "password", default)).Id);
        _users.GetRolesAsync(_user).Returns(new List<string> { Roles.CompanyOwner });
        await Assert.ThrowsAsync<UnauthenticatedException>(() => _identity.LinkGoogleAsync(new("subject", "a@example.com", true, null, "Name"), "password", default));
    }

    private sealed class InlineTransaction : IIdentityTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit) => action();
    }
}