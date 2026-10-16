using System.Net;
using System.Net.Http.Json;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Api.Tests;

public sealed class ProfileEndpointTests
{
    [Fact]
    public async Task Profile_RemainsAccessibleWhenIncompleteAndUpdatesThroughExistingCommand()
    {
        await using var factory = new ApiTestFactory();
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto(null, null));
        factory.Sender.Send(Arg.Any<UpdateTravelerProfileCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success);
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/me/profile")).StatusCode);
        var request = new UpdateTravelerProfileCommand("Name", "01012345678");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/me/profile", request)).StatusCode);
        await factory.Sender.Received().Send(request, Arg.Any<CancellationToken>());
        var problem = await AccountEndpointTests.Problem(await client.PutAsJsonAsync("/api/v1/me/profile", request with { PhoneNumber = "invalid" }), 400, ErrorCodes.ValidationFailed);
        Assert.True(problem.GetProperty("errors").TryGetProperty("phoneNumber", out _));
    }

    [Theory]
    [InlineData("get", null, 401)]
    [InlineData("put", null, 401)]
    [InlineData("get", "CompanyOwner", 403)]
    [InlineData("put", "CompanyEmployee", 403)]
    public async Task Profile_EnforcesIdentity(string method, string? role, int status)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        if (role is not null)
        {
            factory.Authenticate(client, role);
        }

        var response = method == "get" ? await client.GetAsync("/api/v1/me/profile") : await client.PutAsJsonAsync("/api/v1/me/profile", new UpdateTravelerProfileCommand("Name", "01012345678"));
        await AccountEndpointTests.Problem(response, status, status == 401 ? ErrorCodes.Unauthenticated : ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Profile_MissingAccountSaveFailureAndUnexpectedFailureAreMapped()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/me/profile"), 403, ErrorCodes.Forbidden);
        factory.Sender.Send(Arg.Any<UpdateTravelerProfileCommand>(), Arg.Any<CancellationToken>()).Returns(
            (Result<Success>)(Error.Validation(string.Empty, "Unable to save.") with { ReasonCode = ErrorCodes.ProfileUpdateFailed }));
        await AccountEndpointTests.Problem(await client.PutAsJsonAsync("/api/v1/me/profile", new UpdateTravelerProfileCommand("Name", "01012345678")), 400, ErrorCodes.ProfileUpdateFailed);
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns<TravelerProfileDto?>(_ => throw new InvalidOperationException("private SQL"));
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/me/profile"), 500, ErrorCodes.InternalError);
    }

    [Theory]
    [InlineData("CompanyOwner", 200)]
    [InlineData("CompanyEmployee", 200)]
    [InlineData("Traveler", 403)]
    [InlineData(null, 401)]
    public async Task Company_ExposesOnlyExistingPlaceholder(string? role, int status)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        if (role is not null)
        {
            factory.Authenticate(client, role);
        }

        var response = await client.GetAsync("/api/v1/company/status");
        if (status == 200)
        {
            Assert.Contains("\"available\":false", await response.Content.ReadAsStringAsync());
        }
        else
        {
            await AccountEndpointTests.Problem(response, status, status == 401 ? ErrorCodes.Unauthenticated : ErrorCodes.Forbidden);
        }
    }
}