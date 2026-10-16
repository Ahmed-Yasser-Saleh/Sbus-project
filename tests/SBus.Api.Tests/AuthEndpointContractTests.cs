using System.Net.Http.Json;
using System.Text;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;

using Xunit;

namespace SBus.Api.Tests;

public sealed class AuthEndpointContractTests
{
    [Fact]
    public async Task Logout_RequiresJwtAndSharesAuthRateLimit()
    {
        await using var factory = new ApiTestFactory(authLimit: 1);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.PostAsync("/api/v1/auth/logout", null), 401, ErrorCodes.Unauthenticated);
        factory.Authenticate(client, "Traveler");
        var limited = await client.PostAsync("/api/v1/auth/logout", null);
        await AccountEndpointTests.Problem(limited, 429, ErrorCodes.RateLimitExceeded);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    [Fact]
    public async Task Options_RemainsReadableAfterPostQuotaIsExhausted()
    {
        await using var factory = new ApiTestFactory(authLimit: 1);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/auth/login", new { }), 400, ErrorCodes.ValidationFailed);
        var limited = await client.PostAsync("/api/v1/auth/login", null);
        await AccountEndpointTests.Problem(limited, 429, ErrorCodes.RateLimitExceeded);
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.True((await client.GetAsync("/api/v1/auth/options")).IsSuccessStatusCode);
        Assert.True((await client.GetAsync("/api/v1/auth/options")).IsSuccessStatusCode);
    }

    [Theory]
    [InlineData("login")]
    [InlineData("refresh")]
    [InlineData("google")]
    [InlineData("google/link")]
    [InlineData("register")]
    [InlineData("email/resend")]
    [InlineData("email/confirm")]
    [InlineData("password/forgot")]
    [InlineData("password/reset")]
    public async Task AuthBodies_ValidateRejectUnsupportedMediaAndRateLimit(string endpoint)
    {
        await using var factory = new ApiTestFactory(authLimit: 2);
        using var client = factory.Client();
        var path = "/api/v1/auth/" + endpoint;
        await AccountEndpointTests.Problem(await client.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json")), 400, ErrorCodes.ValidationFailed);
        await AccountEndpointTests.Problem(await client.PostAsync(path, new StringContent("invalid")), 415, ErrorCodes.UnsupportedMediaType);
        var limited = await client.PostAsync(path, null);
        await AccountEndpointTests.Problem(limited, 429, ErrorCodes.RateLimitExceeded);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    [Theory]
    [InlineData("login", 401, ErrorCodes.InvalidCredentials)]
    [InlineData("login", 403, ErrorCodes.EmailConfirmationRequired)]
    [InlineData("refresh", 401, ErrorCodes.RefreshInvalid)]
    [InlineData("google", 401, ErrorCodes.GoogleTokenInvalid)]
    [InlineData("google", 401, ErrorCodes.GoogleSignInFailed)]
    [InlineData("google", 503, ErrorCodes.GoogleUnavailable)]
    [InlineData("google/link", 401, ErrorCodes.GoogleLinkInvalid)]
    [InlineData("google/link", 401, ErrorCodes.GoogleLinkFailed)]
    [InlineData("google/link", 401, ErrorCodes.InvalidCredentials)]
    [InlineData("google/link", 403, ErrorCodes.EmailConfirmationRequired)]
    public async Task AuthFailures_PreserveSpecificReasons(string endpoint, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        Exception error = status == 403 ? new ForbiddenException("Confirm email.", code)
            : status == 503 ? new ServiceUnavailableException("Unavailable.", code)
            : new UnauthenticatedException("Unable to authenticate.", code);
        factory.Auth.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(error));
        factory.Auth.RefreshAsync(Arg.Any<RefreshRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(error));
        factory.Auth.GoogleAsync(Arg.Any<GoogleRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileGoogleResult>(error));
        factory.Auth.LinkGoogleAsync(Arg.Any<GoogleLinkRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(error));
        object request = endpoint switch
        {
            "login" => new LoginRequest("a@example.com", "password"),
            "refresh" => new RefreshRequest("refresh"),
            "google" => new GoogleRequest("token"),
            _ => new GoogleLinkRequest("proof", "password"),
        };
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/auth/" + endpoint, request), status, code);
    }
}
