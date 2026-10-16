using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using NSubstitute;
using SBus.Application.Common.Errors;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Time;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetAvailableTrips;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Application.Features.Trips.Queries.SearchTrips;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;
using SBus.Tests.Common;
using Xunit;

namespace SBus.Api.Tests;

public sealed class TripEndpointTests
{
    [Fact]
    public async Task Search_ReturnsEffectiveFiltersAndPagination()
    {
        await using var factory = new ApiTestFactory();
        var today = new DateOnly(2030, 1, 1);
        factory.Sender.Send(Arg.Any<SearchTripsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new TripSearchDto(Direction.CairoToSuez, today, [today], [], 1, 20, false));
        using var client = factory.Client();
        var response = await client.GetAsync("/api/v1/trips");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SBus.Web.Api.Contracts.TripSearchResponse>();
        Assert.Empty(result!.Data);
        Assert.Equal(today, result.Date);
        Assert.False(result.Pagination.HasMore);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("date=invalid")]
    public async Task Search_InvalidQueriesReturnFieldErrors(string query)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/trips?" + query), 400, ErrorCodes.ValidationFailed);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("CompanyOwner", 403)]
    [InlineData("CompanyEmployee", 403)]
    public async Task Details_RequireTraveler(string? role, int status)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        if (role is not null) { factory.Authenticate(client, role); }
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/trips/" + Guid.NewGuid()), status, status == 401 ? ErrorCodes.Unauthenticated : ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Details_PreserveProfileGateAndNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        var path = "/api/v1/trips/" + Guid.NewGuid();
        await AccountEndpointTests.Problem(await client.GetAsync(path), 422, ErrorCodes.ProfileIncomplete);
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Name", "+201012345678"));
        factory.Sender.Send(Arg.Any<GetTripForBookingQuery>(), Arg.Any<CancellationToken>()).Returns(ApplicationErrors.TripNotFound);
        await AccountEndpointTests.Problem(await client.GetAsync(path), 404, ErrorCodes.TripNotFound);
    }

    [Fact]
    public async Task Details_ReturnClosedTripAsDataAndHideUnexpectedFailures()
    {
        await using var factory = new ApiTestFactory();
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Name", "phone"));
        factory.Sender.Send(Arg.Any<GetTripForBookingQuery>(), Arg.Any<CancellationToken>()).Returns(
            new TripBookingDto(Guid.NewGuid(), Direction.CairoToSuez, new(2030, 1, 1), new(9, 0), 150, false, new(0, 0, []), []));
        using var client = factory.Client();
        factory.Authenticate(client, "Traveler");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/trips/" + Guid.NewGuid())).StatusCode);
        factory.Sender.Send(Arg.Any<GetTripForBookingQuery>(), Arg.Any<CancellationToken>()).Returns<Result<TripBookingDto>>(_ => throw new InvalidOperationException("private SQL"));
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/trips/" + Guid.NewGuid()), 500, ErrorCodes.InternalError);
    }

    [Fact]
    public async Task SearchHandler_NormalizesDefaultsAndPaginatesSameAvailableTripsQuery()
    {
        var sender = NSubstitute.Substitute.For<MediatR.ISender>();
        var clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 22, 30, 0, TimeSpan.Zero));
        var today = CairoTime.Today(clock.GetUtcNow());
        var trips = Enumerable.Range(0, 3).Select(_ => new TripSummaryDto(Guid.NewGuid(), Direction.CairoToSuez, today, new(9, 0), 150, 14, 3)).ToList();
        sender.Send(new GetAvailableTripsQuery(Direction.CairoToSuez, today), Arg.Any<CancellationToken>()).Returns(trips);
        var handler = new SearchTripsQueryHandler(sender, clock, Options.Create(new BookingOptions()));
        var result = await handler.Handle(new((Direction)123, today.AddDays(-1), 2, 1), default);
        Assert.Equal(today, result.Value.Date);
        Assert.Equal(Direction.CairoToSuez, result.Value.Direction);
        Assert.Equal(7, result.Value.Days.Count);
        Assert.Equal(trips[1], Assert.Single(result.Value.Trips));
        Assert.True(result.Value.HasMore);
        var empty = await handler.Handle(new(Date: today, Page: 10), default);
        Assert.Empty(empty.Value.Trips);
    }
}
