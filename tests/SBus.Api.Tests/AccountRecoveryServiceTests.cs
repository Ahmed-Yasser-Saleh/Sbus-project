using System.Net.Mail;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Infrastructure.Identity;
using SBus.Web.Services;

using Xunit;

namespace SBus.Api.Tests;

public sealed class AccountRecoveryServiceTests
{
    private readonly UserManager<AppUser> _users;
    private readonly IAccountEmail _email = Substitute.For<IAccountEmail>();
    private readonly AccountRecoveryService _service;
    private readonly AppUser _user = new() { Id = "user" };

    public AccountRecoveryServiceTests()
    {
        _users = Substitute.For<UserManager<AppUser>>(Substitute.For<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(), Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(), Substitute.For<ILogger<UserManager<AppUser>>>());
        _service = new(_users, _email, Substitute.For<ILogger<AccountRecoveryService>>());
        _users.FindByIdAsync("user").Returns(_user);
        _users.FindByEmailAsync("a@example.com").Returns(_user);
    }

    [Fact]
    public async Task Confirmation_DecodesTokenAndUsesIdentity()
    {
        _users.ConfirmEmailAsync(_user, "token").Returns(IdentityResult.Success);
        Assert.True(await _service.ConfirmAsync("user", WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token")), default));
        Assert.False(await _service.ConfirmAsync("user", "!invalid!", default));
        Assert.False(await _service.ConfirmAsync("missing", "code", default));
    }

    [Fact]
    public async Task Reset_RequiresConfirmedPasswordAccountAndIdentitySuccess()
    {
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("token"));
        Assert.False(await _service.ResetAsync("user", code, "password", default));
        _user.EmailConfirmed = true;
        _users.HasPasswordAsync(_user).Returns(true);
        _users.ResetPasswordAsync(_user, "token", "password").Returns(IdentityResult.Success);
        Assert.True(await _service.ResetAsync("user", code, "password", default));
        Assert.False(await _service.ResetAsync("user", "!invalid!", "password", default));
    }

    [Fact]
    public async Task EmailRequests_OnlySendForEligibleAccountsAndHideSmtpFailures()
    {
        await _service.SendConfirmationAsync(" a@example.com ", default);
        await _email.Received(1).SendConfirmationAsync(_user, Arg.Any<CancellationToken>());
        await _service.SendResetAsync("a@example.com", default);
        await _email.DidNotReceiveWithAnyArgs().SendResetAsync(default!, default);
        _user.EmailConfirmed = true;
        _users.HasPasswordAsync(_user).Returns(true);
        _email.SendResetAsync(_user, Arg.Any<CancellationToken>()).Returns(Task.FromException(new SmtpException("Private SMTP diagnostic")));
        await _service.SendResetAsync("a@example.com", default);
        await _service.SendResetAsync("missing@example.com", default);
        await _email.Received(1).SendResetAsync(_user, Arg.Any<CancellationToken>());
    }
}