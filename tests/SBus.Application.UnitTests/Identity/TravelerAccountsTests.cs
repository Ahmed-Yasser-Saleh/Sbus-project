using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Infrastructure.Identity;

using Xunit;

namespace SBus.Application.UnitTests.Identity;

public class TravelerAccountsTests
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly TravelerAccounts _accounts;

    public TravelerAccountsTests()
    {
        _users = Substitute.For<UserManager<AppUser>>(
            Substitute.For<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(), Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(), new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<AppUser>>>());
        _users.FindByEmailAsync(Arg.Any<string>()).Returns(Task.FromResult<AppUser?>(null));
        _users.FindByLoginAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult<AppUser?>(null));
        _signIn = Substitute.For<SignInManager<AppUser>>(
            _users, Substitute.For<IHttpContextAccessor>(), Substitute.For<IUserClaimsPrincipalFactory<AppUser>>(),
            Options.Create(new IdentityOptions()), Substitute.For<ILogger<SignInManager<AppUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(), Substitute.For<IUserConfirmation<AppUser>>());
        _accounts = new TravelerAccounts(_users, _signIn, new InlineTransaction());
    }

    [Fact]
    public async Task Registration_AssignsOnlyTraveler_AndLeavesEmailUnconfirmed()
    {
        _users.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _users.AddToRoleAsync(Arg.Any<AppUser>(), Roles.Traveler).Returns(IdentityResult.Success);

        var user = await _accounts.RegisterAsync(" person@example.com ", "StrongPassword1!", " Mona Ahmed ", "01012345678");

        Assert.NotNull(user);
        Assert.Equal("person@example.com", user.Email);
        Assert.False(user.EmailConfirmed);
        Assert.Equal("Mona Ahmed", user.FullName);
        Assert.Equal("+201012345678", user.PhoneNumber);
        await _users.Received(1).AddToRoleAsync(user, Roles.Traveler);
        await _users.DidNotReceive().AddToRoleAsync(Arg.Any<AppUser>(), Roles.CompanyOwner);
        await _users.DidNotReceive().AddToRoleAsync(Arg.Any<AppUser>(), Roles.CompanyEmployee);
    }

    [Fact]
    public async Task RegistrationInterface_EnforcesIdentityPasswordPolicyBeforeEmailLookup()
    {
        var validator = Substitute.For<IPasswordValidator<AppUser>>();
        validator.ValidateAsync(_users, Arg.Any<AppUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "PasswordRequiresDigit" }));
        _users.PasswordValidators.Add(validator);

        var result = await ((ITravelerRegistration)_accounts)
            .RegisterAsync("person@example.com", "weakpassword", "Mona Ahmed", "01012345678", CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Password", result.TopError.Code);
        await _users.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DuplicateRegistration_DoesNotCreateOrAssignRoles()
    {
        _users.FindByEmailAsync("person@example.com").Returns(new AppUser());

        Assert.Null(await _accounts.RegisterAsync("person@example.com", "StrongPassword1!", "Mona Ahmed", "01012345678"));

        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
        await _users.DidNotReceive().AddToRoleAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("test@gmi")]
    [InlineData("test@gmail..com")]
    public async Task InvalidRegistrationEmail_IsRejectedBeforeIdentityAccess(string email)
    {
        Assert.Null(await _accounts.RegisterAsync(email, "StrongPassword1!", "Mona Ahmed", "01012345678"));
        await _users.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DuplicateRegistrationInterface_ReturnsExplicitEmailError()
    {
        _users.FindByEmailAsync("person@example.com").Returns(new AppUser());

        var result = await ((ITravelerRegistration)_accounts)
            .RegisterAsync(" person@example.com ", "12345", "Mona Ahmed", "01012345678", CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Email", result.TopError.Code);
        Assert.Equal("البريد الإلكتروني مستخدم بالفعل.", result.TopError.Template);
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }

    [Fact]
    public async Task FailedRoleAssignment_DoesNotCommitRegistration()
    {
        var transaction = new RecordingTransaction();
        _users.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        _users.AddToRoleAsync(Arg.Any<AppUser>(), Roles.Traveler).Returns(IdentityResult.Failed());

        var service = new TravelerAccounts(_users, _signIn, transaction);
        Assert.Null(await service.RegisterAsync("person@example.com", "StrongPassword1!", "Mona Ahmed", "01012345678"));
        Assert.False(transaction.Committed);
    }

    [Theory]
    [InlineData("Google", "false")]
    [InlineData("OtherProvider", "true")]
    public async Task UnverifiedOrWrongProvider_DoesNotCreateAccount(string provider, string verified)
    {
        var result = await _accounts.ResolveGoogleAsync(Google("person@gmail.com", verified, provider));

        Assert.Equal(ExternalAccountState.Rejected, result.State);
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>());
    }

    [Theory]
    [InlineData("person@gmail.com", true)]
    [InlineData("person@example.com", false)]
    public async Task FirstGoogleLogin_CreatesTraveler_WithAppropriateEmailTrust(string email, bool confirmed)
    {
        _users.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        _users.AddToRoleAsync(Arg.Any<AppUser>(), Roles.Traveler).Returns(IdentityResult.Success);
        _users.AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);

        var info = Google(email);
        var result = await _accounts.ResolveGoogleAsync(info);

        Assert.Equal(ExternalAccountState.Ready, result.State);
        Assert.Equal(confirmed, result.User!.EmailConfirmed);
        Assert.Equal("Mona Ahmed", result.User.FullName);
        Assert.Null(result.User.PhoneNumber);
        await _users.Received(1).AddToRoleAsync(result.User, Roles.Traveler);
        await _users.Received(1).AddLoginAsync(result.User, info);
    }

    [Fact]
    public async Task ExistingPasswordAccount_RequiresProof_WithoutCreatingOrLinking()
    {
        var user = new AppUser { EmailConfirmed = true };
        _users.FindByEmailAsync("person@gmail.com").Returns(user);
        _users.GetRolesAsync(user).Returns(new List<string> { Roles.Traveler });
        _users.HasPasswordAsync(user).Returns(true);

        Assert.Equal(
            ExternalAccountState.PasswordProofRequired,
            (await _accounts.ResolveGoogleAsync(Google("person@gmail.com"))).State);
        await _users.DidNotReceive().AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>());
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>());
    }

    [Theory]
    [InlineData(Roles.CompanyOwner)]
    [InlineData(Roles.CompanyEmployee)]
    [InlineData(Roles.Office)]
    public async Task GoogleCannotLinkPrivilegedAccountByEmail(string role)
    {
        var user = new AppUser { EmailConfirmed = true };
        _users.FindByEmailAsync("person@gmail.com").Returns(user);
        _users.GetRolesAsync(user).Returns(new List<string> { role });
        _users.HasPasswordAsync(user).Returns(true);

        Assert.Equal(ExternalAccountState.Rejected, (await _accounts.ResolveGoogleAsync(Google("person@gmail.com"))).State);
    }

    [Fact]
    public async Task WrongPassword_DoesNotLinkGoogle_AndEnablesLockout()
    {
        var user = ExistingTraveler();
        _signIn.CheckPasswordSignInAsync(user, "wrong", true).Returns(SignInResult.Failed);

        Assert.Null(await _accounts.LinkGoogleWithPasswordAsync(Google("person@gmail.com"), "wrong"));

        await _signIn.Received().CheckPasswordSignInAsync(user, "wrong", true);
        await _users.DidNotReceive().AddLoginAsync(Arg.Any<AppUser>(), Arg.Any<UserLoginInfo>());
    }

    [Fact]
    public async Task CorrectPassword_LinksGoogleToTheSameAccount()
    {
        var user = ExistingTraveler();
        _signIn.CheckPasswordSignInAsync(user, "correct", true).Returns(SignInResult.Success);
        _users.AddLoginAsync(user, Arg.Any<UserLoginInfo>()).Returns(IdentityResult.Success);

        Assert.Same(user, await _accounts.LinkGoogleWithPasswordAsync(Google("person@gmail.com"), "correct"));
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>());
    }

    [Fact]
    public async Task LinkedGoogleSubject_ReusesAccount()
    {
        var user = new AppUser();
        _users.FindByLoginAsync("Google", "google-subject").Returns(user);

        Assert.Same(user, (await _accounts.ResolveGoogleAsync(Google("person@gmail.com"))).User);
        await _users.DidNotReceive().CreateAsync(Arg.Any<AppUser>());
    }

    [Theory]
    [InlineData("+201012345678", true)]
    [InlineData("+201112345678", false)]
    public async Task ProfileUpdate_UsesCurrentAccount_AndPreservesLoginIdentifier(string newPhone, bool phoneConfirmed)
    {
        var user = new AppUser
        {
            Id = "current-traveler",
            UserName = "person@example.com",
            Email = "person@example.com",
            FullName = "Old Name",
            PhoneNumber = "+201012345678",
            PhoneNumberConfirmed = true,
        };
        var current = Substitute.For<IUser>();
        current.Id.Returns(user.Id);
        current.IsTraveler.Returns(true);
        _users.FindByIdAsync(user.Id).Returns(user);
        _users.UpdateAsync(user).Returns(IdentityResult.Success);
        var profiles = new TravelerProfiles(_users, current);
        var result = await profiles.UpdateCurrentAsync("New Name", newPhone, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal(newPhone, user.PhoneNumber);
        Assert.Equal(phoneConfirmed, user.PhoneNumberConfirmed);
        Assert.Equal("person@example.com", user.UserName);
        Assert.Equal("person@example.com", user.Email);
        await _users.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ProfileService_NonTravelerCannotReadOrEditAnAccount()
    {
        var current = Substitute.For<IUser>();
        current.Id.Returns("company-owner");
        current.IsTraveler.Returns(false);
        var profiles = new TravelerProfiles(_users, current);
        Assert.Null(await profiles.GetCurrentAsync(CancellationToken.None));
        Assert.True((await profiles.UpdateCurrentAsync("Name", "+201012345678", CancellationToken.None)).IsError);
        await _users.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        await _users.DidNotReceive().UpdateAsync(Arg.Any<AppUser>());
    }

    private AppUser ExistingTraveler()
    {
        var user = new AppUser { EmailConfirmed = true };
        _users.FindByEmailAsync("person@gmail.com").Returns(user);
        _users.GetRolesAsync(user).Returns(new List<string> { Roles.Traveler });
        return user;
    }

    private static ExternalLoginInfo Google(string email, string verified = "true", string provider = "Google") =>
        new(
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, "Mona Ahmed"),
                new Claim(TravelerAccounts.VerifiedEmailClaim, verified),
            ])), provider, "google-subject", "Google");

    private sealed class InlineTransaction : IIdentityTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit) => action();
    }

    private sealed class RecordingTransaction : IIdentityTransaction
    {
        public bool Committed { get; private set; }
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit)
        {
            var result = await action();
            Committed = commit(result);
            return result;
        }
    }
}

