using System.Net.Http.Json;

using MediatR;

using NSubstitute;
using NSubstitute.Extensions;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetMyTickets;
using SBus.Application.Features.MobileAuth;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.SearchTrips;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Api.Tests;

public sealed class EndpointFailureTests
{
    private static readonly Guid Id = Guid.NewGuid();

    public static IEnumerable<object[]> Endpoints()
    {
        foreach (var name in new[] { "search", "trip", "booking", "ticket", "cancel", "receipt", "mine", "profileGet", "profilePut", "register", "resend", "forgot", "confirmEmail", "reset", "login", "refresh", "google", "link", "logout" })
        {
            yield return [name];
        }

        foreach (var row in OfficeEndpointTests.Endpoints)
        {
            yield return [row[0] + " " + row[1]];
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EndpointFailures_ReturnSafe500AndUnavailable503(string endpoint)
    {
        await using var factory = new ApiTestFactory();
        using var client = BookingEndpointTests.Traveler(factory);
        if (endpoint.Contains(' '))
        {
            factory.Authenticate(client, "Office");
        }

        Configure(new InvalidOperationException("private SQL connection"));
        var response = await Send(client, endpoint);
        var problem = await AccountEndpointTests.Problem(response, 500, ErrorCodes.InternalError);
        Assert.DoesNotContain("private SQL", problem.ToString());
        Configure(new ServiceUnavailableException("Temporarily unavailable."));
        await AccountEndpointTests.Problem(await Send(client, endpoint), 503, ErrorCodes.ApiUnavailable);

        void Configure(Exception exception)
        {
            Throw<Updated>();
            Throw<Success>();
            Throw<BookingCreatedDto>();
            Throw<TicketDto>();
            Throw<ReceiptFileDto>();
            Throw<TripBookingDto>();
            Throw<TripSearchDto>();
            Throw<TripManifestDto>();
            Throw<List<OfficeTripDto>>();
            Throw<List<PendingPaymentDto>>();
            factory.Sender.Configure().Send(Arg.Any<IRequest<IReadOnlyList<MyTicketDto>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<IReadOnlyList<MyTicketDto>>(exception));
            if (endpoint == "profileGet")
            {
                factory.Profiles.Configure().GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<TravelerProfileDto?>(exception));
            }

            factory.Auth.Configure().LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(exception));
            factory.Auth.Configure().RefreshAsync(Arg.Any<RefreshRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(exception));
            factory.Auth.Configure().GoogleAsync(Arg.Any<GoogleRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileGoogleResult>(exception));
            factory.Auth.Configure().LinkGoogleAsync(Arg.Any<GoogleLinkRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<MobileTokens>(exception));
            factory.Auth.Configure().LogoutAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(exception));
            factory.Recovery.Configure().SendConfirmationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(exception));
            factory.Recovery.Configure().SendResetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(exception));
            factory.Recovery.Configure().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(exception));
            factory.Recovery.Configure().ResetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(exception));

            void Throw<T>() => factory.Sender.Configure().Send(Arg.Any<IRequest<Result<T>>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Result<T>>(exception));
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task DisabledApi_AllFeatureEndpointsReturn503WithoutDispatch(string endpoint)
    {
        await using var factory = new ApiTestFactory(apiEnabled: false);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await Send(client, endpoint), 503, ErrorCodes.ApiUnavailable);
        Assert.Empty(factory.Sender.ReceivedCalls());
    }

    [Theory]
    [InlineData("/api/v1/auth/options")]
    [InlineData("/api/v1/company/status")]
    public async Task DisabledApi_StaticEndpointsReturn503(string path)
    {
        await using var factory = new ApiTestFactory(apiEnabled: false);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.GetAsync(path), 503, ErrorCodes.ApiUnavailable);
    }

    [Theory]
    [InlineData("ticket")]
    [InlineData("cancel")]
    [InlineData("receipt")]
    public async Task OversizedTicketToken_ReturnsFieldValidation(string endpoint)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var response = await SendTicket(client, endpoint, new string('x', 129));
        var problem = await AccountEndpointTests.Problem(response, 400, ErrorCodes.ValidationFailed);
        Assert.True(problem.GetProperty("errors").TryGetProperty("token", out _));
    }

    [Fact]
    public async Task MyTickets_RequiresCompleteProfileAndTripGuidMustBeValid()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/me/tickets"), 422, ErrorCodes.ProfileIncomplete);
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Name", "01012345678"));
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/trips/invalid"), 400, ErrorCodes.ValidationFailed);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string endpoint)
    {
        if (endpoint.Contains(' '))
        {
            var pieces = endpoint.Split(' ', 2);
            var path = "/api/v1/office" + pieces[1];
            object body = path.EndsWith("/bookings", StringComparison.Ordinal) ? new OfficeBookingRequest("Name", "01012345678", Id, Guid.NewGuid(), [1])
                : path.EndsWith("/assignment", StringComparison.Ordinal) ? new TripAssignmentRequest(Id, Id)
                : new RejectPaymentRequest("Incorrect amount");
            return pieces[0] switch
            {
                "GET" => client.GetAsync(path),
                "PUT" => client.PutAsJsonAsync(path, body),
                _ => client.PostAsJsonAsync(path, body),
            };
        }

        return endpoint switch
        {
            "search" => client.GetAsync("/api/v1/trips"),
            "trip" => client.GetAsync("/api/v1/trips/" + Id),
            "booking" => client.PostAsJsonAsync("/api/v1/bookings", new { tripId = Id, pickupStopId = Id, dropoffStopId = Guid.NewGuid(), seatNumbers = new[] { 1 } }),
            "ticket" or "cancel" or "receipt" => SendTicket(client, endpoint, "token"),
            "mine" => client.GetAsync("/api/v1/me/tickets"),
            "profileGet" => client.GetAsync("/api/v1/me/profile"),
            "profilePut" => client.PutAsJsonAsync("/api/v1/me/profile", new { fullName = "Name", phoneNumber = "01012345678" }),
            "register" => client.PostAsJsonAsync("/api/v1/auth/register", new RegisterTravelerCommand("a@example.com", "password", "password", "Name", "01012345678")),
            "resend" => client.PostAsJsonAsync("/api/v1/auth/email/resend", new { email = "a@example.com" }),
            "forgot" => client.PostAsJsonAsync("/api/v1/auth/password/forgot", new { email = "a@example.com" }),
            "confirmEmail" => client.PostAsJsonAsync("/api/v1/auth/email/confirm", new { userId = "user", code = "code" }),
            "reset" => client.PostAsJsonAsync("/api/v1/auth/password/reset", new { userId = "user", code = "code", password = "password", confirmPassword = "password" }),
            "login" => client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("a@example.com", "password")),
            "refresh" => client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest("token")),
            "google" => client.PostAsJsonAsync("/api/v1/auth/google", new GoogleRequest("token")),
            "link" => client.PostAsJsonAsync("/api/v1/auth/google/link", new GoogleLinkRequest("proof", "password")),
            _ => client.PostAsync("/api/v1/auth/logout", null),
        };
    }

    private static async Task<HttpResponseMessage> SendTicket(HttpClient client, string endpoint, string token)
    {
        var path = "/api/v1/tickets/" + token;
        if (endpoint == "ticket")
        {
            return await client.GetAsync(path);
        }

        if (endpoint == "cancel")
        {
            return await client.PostAsync(path + "/cancel", null);
        }

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([0x89, 0x50, 0x4e, 0x47]), "receipt", "receipt.png");
        return await client.PostAsync(path + "/receipt", form);
    }
}