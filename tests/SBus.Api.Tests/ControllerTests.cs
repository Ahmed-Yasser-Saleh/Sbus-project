using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Features.MobileAuth;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Authentication;
using SBus.Web.Api.Controllers;

using Xunit;

namespace SBus.Api.Tests;

public sealed class ControllerTests
{
    [Theory]
    [InlineData("login")]
    [InlineData("refresh")]
    [InlineData("google")]
    [InlineData("google/link")]
    public async Task AuthActions_ReturnDtoEnvelopeThroughRealPipeline(string action)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var tokens = new MobileTokens("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(10), DateTimeOffset.UtcNow.AddDays(30),
            new MobileAccount("user", [Roles.Traveler], MobileAuthService.Hash("stamp"), true));
        factory.Auth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(tokens);
        factory.Auth.RefreshAsync(Arg.Any<RefreshRequest>(), Arg.Any<CancellationToken>()).Returns(tokens);
        factory.Auth.GoogleAsync(Arg.Any<GoogleRequest>(), Arg.Any<CancellationToken>()).Returns(new MobileGoogleResult(MobileGoogleState.Ready, tokens, null));
        factory.Auth.LinkGoogleAsync(Arg.Any<GoogleLinkRequest>(), Arg.Any<CancellationToken>()).Returns(tokens);
        object request = action switch
        {
            "login" => new LoginRequest("a@example.com", "password"),
            "refresh" => new RefreshRequest("refresh"),
            "google" => new GoogleRequest("id-token"),
            _ => new GoogleLinkRequest("proof", "password"),
        };
        var response = await client.PostAsJsonAsync("/api/v1/auth/" + action, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"data\":", body);
        Assert.Contains("\"accessToken\":\"access\"", body);
        Assert.DoesNotContain("securityStampHash", body);
    }

    [Fact]
    public async Task Logout_DerivesUserAndSessionFromPrincipal()
    {
        var auth = Substitute.For<IMobileAuthService>();
        var family = Guid.NewGuid();
        var controller = new MobileAuthController(auth, Options.Create(new MobileAuthOptions()))
        {
            ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "current-user"), new Claim(MobileClaims.FamilyId, family.ToString())], "Bearer")),
                },
            },
        };
        Assert.True((await controller.Logout(default)).Data);
        await auth.Received().LogoutAsync("current-user", family, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Options_ExposeCapabilitiesWithoutSecretConfiguration()
    {
        var controller = new MobileAuthController(Substitute.For<IMobileAuthService>(), Options.Create(new MobileAuthOptions { GoogleClientIds = ["client-id"] }));
        var response = controller.Options(Options.Create(new Microsoft.AspNetCore.Identity.IdentityOptions()));
        Assert.True(response.Data.GoogleEnabled);
        Assert.Equal(5, response.Data.PasswordMinLength);
        Assert.Equal(128, response.Data.PasswordMaxLength);
    }
}