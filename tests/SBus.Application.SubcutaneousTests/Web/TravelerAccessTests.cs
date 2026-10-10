using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Domain.Common.Results;
using SBus.Infrastructure.Identity;
using SBus.Web.Services;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Web;

// This factory never opens a database or invokes migration/seed code.
public class TravelerAccessTests
{
    [Theory]
    [InlineData("12345", true)]
    [InlineData("abcde", true)]
    [InlineData("!!!!!", true)]
    [InlineData("1234", false)]
    public async Task IdentityPasswordPolicy_OnlyRequiresFiveCharacters(string password, bool accepted)
    {
        await using var factory = new AuthOnlyFactory();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.NotEmpty(users.PasswordValidators);

        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, new AppUser(), password);
            Assert.Equal(accepted, result.Succeeded);
        }
    }

    [Theory]
    [InlineData("new-user", true)]
    [InlineData("new-user", false)]
    [InlineData(null, true)]
    public async Task Registration_OnlyRedirectsWhenAccountWasCreated(string? userId, bool requireConfirmation)
    {
        var registration = Substitute.For<ITravelerRegistration>();
        var confirmation = Substitute.For<IRegistrationConfirmationSender>();
        registration.RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TravelerRegistrationOutcome>>(userId is null
                ? Error.Validation("Email", "البريد الإلكتروني مستخدم بالفعل.")
                : new TravelerRegistrationOutcome(userId)));
        await using var factory = new AuthOnlyFactory(registration, confirmation, requireConfirmation);
        using var client = Client(factory);
        var html = await client.GetStringAsync("/Account/Register");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["Email"] = "person@example.com",
            ["Password"] = "StrongPassword1!",
            ["ConfirmPassword"] = "StrongPassword1!",
            ["Role"] = Roles.CompanyOwner,
        });
        var response = await client.PostAsync("/Account/Register", form);

        await registration.Received(1).RegisterAsync("person@example.com", "StrongPassword1!", Arg.Any<CancellationToken>());
        if (userId is not null)
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location!.OriginalString);
            await confirmation.Received(1).SendAsync(userId, Arg.Any<CancellationToken>());
            var loginHtml = WebUtility.HtmlDecode(await client.GetStringAsync("/Account/Login"));
            var expectedMessage = requireConfirmation
                ? "تم إنشاء حسابك. يجب تأكيد بريدك الإلكتروني من رابط التأكيد قبل تسجيل الدخول."
                : "تم إنشاء حسابك. يمكنك تسجيل الدخول الآن.";
            Assert.Contains(expectedMessage, loginHtml);
            if (requireConfirmation)
            {
                Assert.Contains("/Account/ResendConfirmation", loginHtml);
            }

            var refreshedHtml = WebUtility.HtmlDecode(await client.GetStringAsync("/Account/Login"));
            Assert.DoesNotContain("تم إنشاء حسابك.", refreshedHtml);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Headers.Location);
            var responseHtml = await response.Content.ReadAsStringAsync();
            var emailErrors = Regex.Matches(
                WebUtility.HtmlDecode(responseHtml),
                Regex.Escape("البريد الإلكتروني مستخدم بالفعل."));
            Assert.Single(emailErrors);
            Assert.Contains("text-danger d-block mt-1", responseHtml);
            Assert.DoesNotContain("value=\"StrongPassword1!\"", responseHtml);
            await confirmation.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    [Theory]
    [InlineData("test@gmi")]
    [InlineData("test@gmail..com")]
    public async Task InvalidRegistrationEmail_ShowsValidationError_WithoutDatabaseAccess(string email)
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory);
        var html = await client.GetStringAsync("/Account/Register");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["Email"] = email,
            ["Password"] = "StrongPassword1!",
            ["ConfirmPassword"] = "StrongPassword1!",
        });
        var response = await client.PostAsync("/Account/Register", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("Enter a valid email address", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GoogleLogin_RequestsAccountSelection_WithStateAndPkce()
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory);
        var html = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
        });
        var response = await client.PostAsync("/Account/ExternalLogin", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("accounts.google.com", location.Host);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("select_account", query["prompt"].ToString());
        Assert.NotEmpty(query["state"].ToString());
        Assert.NotEmpty(query["code_challenge"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
    }

    [Theory]
    [InlineData("/MyTickets")]
    [InlineData("/trip/00000000-0000-0000-0000-000000000001")]
    [InlineData("/Account/ComingSoon")]
    [InlineData("/Office")]
    public async Task Anonymous_ProtectedEndpoint_UsesSharedLogin(string path)
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location!.AbsolutePath);
    }

    [Theory]
    [InlineData(Roles.CompanyOwner)]
    [InlineData(Roles.CompanyEmployee)]
    public async Task CompanyRole_CanOnlyAccessPlaceholder_NotTravelerOrOfficeFeatures(string role)
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory, role);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/ComingSoon")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/MyTickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Office/Payments")).StatusCode);
        var landing = await client.GetAsync("/Account/Login");
        Assert.Equal("/Account/ComingSoon", landing.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Traveler_CannotAccessCompanyOrOffice()
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory, Roles.Traveler);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Account/ComingSoon")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Office")).StatusCode);
        Assert.Equal("/", (await client.GetAsync("/Account/Login")).Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Account/ExternalLogin")]
    [InlineData("/Account/LinkGoogle")]
    [InlineData("/Account/ForgotPassword")]
    [InlineData("/Account/ResetPassword")]
    [InlineData("/Account/Logout")]
    public async Task AccountPost_WithoutAntiforgery_IsRejected(string path)
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory, Roles.Traveler);
        var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void LandingPrecedence_DoesNotGrantCompanyUsersLegacyAccess()
    {
        Assert.Equal("/Account/ComingSoon", AccountLanding.Path(new List<string> { Roles.Traveler, Roles.Office, Roles.CompanyOwner }));
        Assert.Equal("/Office", AccountLanding.Path(new List<string> { Roles.Office, Roles.Traveler }));
    }

    [Fact]
    public async Task AccountPosts_AreRateLimited_WithoutLimitingPageReads()
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory);
        for (var i = 0; i < 25; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
        }

        for (var i = 0; i < 20; i++)
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>());

            // Antiforgery rejects the form, but POST requests still consume the limit.
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Account/Login", form)).StatusCode);
        }

        using var blockedForm = new FormUrlEncodedContent(new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/Account/Login", blockedForm)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
    }

    [Fact]
    public async Task CompanyRole_CannotInheritTravelerOrOfficePermissions()
    {
        await using var factory = new AuthOnlyFactory();
        using var client = Client(factory, $"{Roles.Traveler},{Roles.Office},{Roles.CompanyOwner}");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/MyTickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Office")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/ComingSoon")).StatusCode);
    }

    private static HttpClient Client(AuthOnlyFactory factory, string? role = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add("Test-Role", role);
        }

        return client;
    }

    private sealed class AuthOnlyFactory(
        ITravelerRegistration? registration = null,
        IRegistrationConfirmationSender? confirmation = null,
        bool requireConfirmation = true) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused");
            builder.UseSetting("AppSettings:InitializeDatabaseOnStartup", "false");
            builder.UseSetting("Authentication:RequireConfirmedEmail", requireConfirmation.ToString());
            builder.UseSetting("Authentication:Email:PublicOrigin", "https://localhost");
            builder.UseSetting("Authentication:Email:Host", "unused");
            builder.UseSetting("Authentication:Email:From", "test@example.com");
            builder.UseSetting("Authentication:Google:ClientId", "test-client");
            builder.UseSetting("Authentication:Google:ClientSecret", "test-secret");
            builder.ConfigureTestServices(services =>
            {
                if (registration is not null)
                {
                    services.AddSingleton(registration);
                }

                if (confirmation is not null)
                {
                    services.AddSingleton(confirmation);
                }

                foreach (var job in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                {
                    services.Remove(job);
                }

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultForbidScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["Test-Role"].ToString();
            if (string.IsNullOrEmpty(role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = role.Split(',').Select(r => new Claim(ClaimTypes.Role, r))
                .Append(new Claim(ClaimTypes.NameIdentifier, "test-user"));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}

