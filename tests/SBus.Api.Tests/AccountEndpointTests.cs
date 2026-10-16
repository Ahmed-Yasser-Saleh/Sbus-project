using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Features.Accounts;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Api.Tests;

public sealed class AccountEndpointTests
{
    [Fact]
    public async Task Registration_UsesExistingCommandAndReturnsConfirmationState()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<RegisterTravelerCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success);
        using var client = factory.Client();
        var request = new RegisterTravelerCommand("a@example.com", "password", "password", "Name", "01012345678");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/register", request)).StatusCode);
        await factory.Sender.Received().Send(request, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "VALIDATION_FAILED")]
    [InlineData("AUTH_EMAIL_ALREADY_REGISTERED", "AUTH_EMAIL_ALREADY_REGISTERED")]
    [InlineData("AUTH_REGISTRATION_FAILED", "AUTH_REGISTRATION_FAILED")]
    public async Task Registration_ValidationIncludesEmailFieldAndStableReason(string reason, string expected)
    {
        await using var factory = new ApiTestFactory();
        Result<Success> failure = Error.Validation("Email", "Invalid account.") with { ReasonCode = reason.Length == 0 ? null : reason };
        factory.Sender.Send(Arg.Any<RegisterTravelerCommand>(), Arg.Any<CancellationToken>()).Returns(failure);
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterTravelerCommand("a@example.com", "password", "password", "Name", "01012345678"));
        var problem = await Problem(response, 400, expected);
        Assert.True(problem.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Theory]
    [InlineData("email/resend")]
    [InlineData("password/forgot")]
    public async Task EmailRequests_PreserveGenericAcknowledgementAndValidation(string endpoint)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/" + endpoint, new EmailRequest("unknown@example.com"))).StatusCode);
        await Problem(await client.PostAsJsonAsync("/api/v1/auth/" + endpoint, new EmailRequest("invalid")), 400, ErrorCodes.ValidationFailed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Confirmation_ValidOrExpiredTokenHasExpectedResult(bool valid)
    {
        await using var factory = new ApiTestFactory();
        factory.Recovery.ConfirmAsync("user", "code", Arg.Any<CancellationToken>()).Returns(valid);
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/v1/auth/email/confirm", new ConfirmEmailRequest("user", "code"));
        if (valid)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            await Problem(response, 400, ErrorCodes.ConfirmationInvalid);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reset_UsesIdentityOutcomeAndRejectsInvalidToken(bool valid)
    {
        await using var factory = new ApiTestFactory();
        factory.Recovery.ResetAsync("user", "code", "password", Arg.Any<CancellationToken>()).Returns(valid);
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/v1/auth/password/reset", new ResetPasswordRequest("user", "code", "password", "password"));
        if (valid)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            await Problem(response, 400, ErrorCodes.PasswordResetInvalid);
        }
    }

    [Fact]
    public async Task Reset_PasswordMismatchDoesNotReachIdentity()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await Problem(await client.PostAsJsonAsync("/api/v1/auth/password/reset", new ResetPasswordRequest("user", "code", "password", "different")), 400, ErrorCodes.ValidationFailed);
        await factory.Recovery.DidNotReceiveWithAnyArgs().ResetAsync(default!, default!, default!, default);
    }

    internal static async Task<JsonElement> Problem(HttpResponseMessage response, int status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(body);
        var problem = json.RootElement.Clone();
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        return problem;
    }
}