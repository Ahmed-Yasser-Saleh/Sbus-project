using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Infrastructure.Identity;
using SBus.Web.Pages.Account;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Web;

public class LoginConfirmationTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task UnconfirmedLogin_RequiresPasswordProofAndHonorsLockout(
        bool correctPassword, bool lockedOut, bool showConfirmation)
    {
        var store = Substitute.For<IUserStore<AppUser>, IUserLockoutStore<AppUser>>();
        var options = Options.Create(new IdentityOptions());
        options.Value.SignIn.RequireConfirmedEmail = true;
        var users = Substitute.For<UserManager<AppUser>>(
            store, options, new PasswordHasher<AppUser>(), Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(), new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<AppUser>>>());
        var signIn = Substitute.For<SignInManager<AppUser>>(
            users, Substitute.For<IHttpContextAccessor>(), Substitute.For<IUserClaimsPrincipalFactory<AppUser>>(),
            options, Substitute.For<ILogger<SignInManager<AppUser>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(), Substitute.For<IUserConfirmation<AppUser>>());
        var user = new AppUser { Email = "person@example.com", EmailConfirmed = false };
        users.SupportsUserLockout.Returns(true);
        users.FindByEmailAsync(user.Email).Returns(user);
        users.IsLockedOutAsync(user).Returns(lockedOut);
        users.GetLockoutEnabledAsync(user).Returns(true);
        users.CheckPasswordAsync(user, "12345").Returns(correctPassword);
        users.AccessFailedAsync(user).Returns(IdentityResult.Success);
        signIn.GetExternalAuthenticationSchemesAsync().Returns(Array.Empty<AuthenticationScheme>());
        signIn.PasswordSignInAsync(user, "12345", false, true).Returns(SignInResult.NotAllowed);
        var page = new LoginModel(signIn, users) { Email = user.Email, Password = "12345" };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(showConfirmation, page.EmailConfirmationRequired);
        Assert.Equal(showConfirmation, page.ModelState.IsValid);
        Assert.Empty(page.Password);
        await users.Received(lockedOut ? 0 : 1).CheckPasswordAsync(user, "12345");
        await users.Received(!lockedOut && !correctPassword ? 1 : 0).AccessFailedAsync(user);
        await signIn.DidNotReceive().SignInAsync(Arg.Any<AppUser>(), Arg.Any<bool>(), Arg.Any<string>());
    }
}
