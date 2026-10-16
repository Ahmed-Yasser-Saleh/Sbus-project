using System.Net;
using System.Net.Http.Json;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Api.Tests;

public sealed class BookingEndpointTests
{
    [Fact]
    public async Task Create_UsesExistingCommandAndReturnsTicketToken()
    {
        await using var factory = new ApiTestFactory();
        var request = Request();
        factory.Sender.Send(Arg.Any<CreateOnlineBookingCommand>(), Arg.Any<CancellationToken>()).Returns(new BookingCreatedDto(Guid.NewGuid(), "ticket-token"));
        using var client = Traveler(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/bookings", request)).StatusCode);
        await factory.Sender.Received().Send(
            Arg.Is<CreateOnlineBookingCommand>(command => command.TripId == request.TripId
            && command.PickupStopId == request.PickupStopId && command.DropoffStopId == request.DropoffStopId
            && command.SeatNumbers.SequenceEqual(request.SeatNumbers) && !command.UpdateAccountDetails), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 404, "TRIP_NOT_FOUND")]
    [InlineData(1, 409, "BOOKING_SEAT_UNAVAILABLE")]
    [InlineData(2, 409, "BOOKING_SEAT_UNAVAILABLE")]
    [InlineData(3, 409, "TRIP_DEPARTED")]
    [InlineData(4, 409, "TRIP_NOT_BOOKABLE")]
    [InlineData(5, 400, "BOOKING_STOPS_NOT_ON_ROUTE")]
    [InlineData(6, 400, "BOOKING_STOP_ORDER_INVALID")]
    [InlineData(7, 400, "BOOKING_SEATS_NOT_IN_LAYOUT")]
    [InlineData(8, 400, "PROFILE_UPDATE_FAILED")]
    public async Task Create_MapsBusinessFailures(int scenario, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        var error = scenario switch
        {
            0 => ApplicationErrors.TripNotFound,
            1 => ApplicationErrors.SeatsTaken([1]),
            2 => ApplicationErrors.SeatsJustTaken,
            3 => ApplicationErrors.TripDeparted,
            4 => ApplicationErrors.TripNotBookable,
            5 => ApplicationErrors.StopsNotOnRoute,
            6 => ApplicationErrors.PickupAfterDropoff,
            7 => ApplicationErrors.SeatsNotInLayout([1]),
            _ => Error.Validation(string.Empty, "Unable to save.") with { ReasonCode = ErrorCodes.ProfileUpdateFailed },
        };
        factory.Sender.Send(Arg.Any<CreateOnlineBookingCommand>(), Arg.Any<CancellationToken>()).Returns((Result<BookingCreatedDto>)error);
        using var client = Traveler(factory);
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request()), status, code);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("CompanyOwner", 403)]
    [InlineData("CompanyEmployee", 403)]
    public async Task Create_RejectsWrongIdentity(string? role, int status)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        if (role is not null)
        {
            factory.Authenticate(client, role);
        }

        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request()), status, status == 401 ? ErrorCodes.Unauthenticated : ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Create_PreservesProfileGateAndSeatValidation()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request()), 422, ErrorCodes.ProfileIncomplete);
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Name", "phone"));
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request() with { SeatNumbers = [] }), 400, ErrorCodes.ValidationFailed);
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request() with { SeatNumbers = [1, 1] }), 400, ErrorCodes.ValidationFailed);
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/bookings", Request() with { SeatNumbers = [1, 2, 3, 4, 5] }), 400, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Create_UnexpectedFailureIsSafeAndRateLimitHasRetryAfter()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<CreateOnlineBookingCommand>(), Arg.Any<CancellationToken>()).Returns<Result<BookingCreatedDto>>(_ => throw new InvalidOperationException("private SQL"));
        using var client = Traveler(factory);
        var response = await client.PostAsJsonAsync("/api/v1/bookings", Request());
        await AccountEndpointTests.Problem(response, 500, ErrorCodes.InternalError);
        for (var index = 0; index < 9; index++)
        {
            await client.PostAsJsonAsync("/api/v1/bookings", Request() with { SeatNumbers = [] });
        }

        response = await client.PostAsJsonAsync("/api/v1/bookings", Request());
        await AccountEndpointTests.Problem(response, 429, ErrorCodes.RateLimitExceeded);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    internal static HttpClient Traveler(ApiTestFactory factory)
    {
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Name", "+201012345678"));
        var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        return client;
    }

    private static CreateOnlineBookingCommand Request() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [1]);
}
