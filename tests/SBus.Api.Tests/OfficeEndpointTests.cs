using System.Net;
using System.Net.Http.Json;

using MediatR;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Models;
using SBus.Application.Features.Bookings.Commands.CancelBookingByOffice;
using SBus.Application.Features.Bookings.Commands.ConfirmPayment;
using SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;
using SBus.Application.Features.Bookings.Commands.RejectPayment;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetPendingPayments;
using SBus.Application.Features.Bookings.Queries.GetReceipt;
using SBus.Application.Features.Trips.Commands.UpdateTripAssignment;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetOfficeDay;
using SBus.Application.Features.Trips.Queries.GetTripManifest;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Api.Tests;

public sealed class OfficeEndpointTests
{
    private static readonly Guid Id = Guid.NewGuid();

    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", "/day" }, { "GET", $"/trips/{Id}/manifest" },
        { "POST", $"/trips/{Id}/bookings" }, { "PUT", $"/trips/{Id}/assignment" },
        { "POST", $"/bookings/{Id}/cancel" }, { "GET", "/payments" },
        { "POST", $"/bookings/{Id}/payment/confirm" }, { "POST", $"/bookings/{Id}/payment/reject" },
        { "GET", $"/bookings/{Id}/receipt" },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EveryOfficeEndpoint_RequiresOfficeAndExcludesCompanyRoles(string method, string path)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await Problem(401, ErrorCodes.Unauthenticated);
        foreach (var roles in new[] { new[] { "Traveler" }, new[] { "CompanyOwner" }, new[] { "CompanyEmployee" }, new[] { "Office", "CompanyOwner" } })
        {
            factory.Authenticate(client, roles);
            await Problem(403, ErrorCodes.Forbidden);
        }

        async Task Problem(int status, string code)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/office" + path);
            await AccountEndpointTests.Problem(await client.SendAsync(request), status, code);
        }
    }

    [Fact]
    public async Task DashboardAndManifest_ReturnSharedReadModels()
    {
        await using var factory = new ApiTestFactory();
        var date = new DateOnly(2030, 1, 2);
        factory.Sender.Send(new GetOfficeDayQuery(date), Arg.Any<CancellationToken>()).Returns((Result<List<OfficeTripDto>>)new List<OfficeTripDto>());
        factory.Sender.Send(new GetPendingPaymentsQuery(), Arg.Any<CancellationToken>()).Returns((Result<List<PendingPaymentDto>>)new List<PendingPaymentDto> { Payment(), Payment() });
        var manifest = new TripManifestDto(Id, Direction.CairoToSuez, date, new TimeOnly(9, 0), 150, Id, "plate", Id, "driver", "01012345678", false, new(1, 1, []), [], []);
        factory.Sender.Send(new GetTripManifestQuery(Id), Arg.Any<CancellationToken>()).Returns((Result<TripManifestDto>)manifest);
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var dashboard = (await client.GetFromJsonAsync<ApiResponse<OfficeDayResponse>>("/api/v1/office/day?date=2030-01-02"))!.Data;
        Assert.Equal(date, dashboard.Date);
        Assert.Equal(2, dashboard.PendingPayments);
        Assert.Equal(manifest, (await client.GetFromJsonAsync<ApiResponse<TripManifestDto>>($"/api/v1/office/trips/{Id}/manifest"))!.Data with { SeatMap = manifest.SeatMap, Stops = manifest.Stops, Bookings = manifest.Bookings });
    }

    [Fact]
    public async Task OfficeCommands_MapRouteIdAndUseExistingValidators()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<CreateOfficeBookingCommand>(), Arg.Any<CancellationToken>()).Returns((Result<BookingCreatedDto>)new BookingCreatedDto(Id, "token"));
        factory.Sender.Send(Arg.Any<UpdateTripAssignmentCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        factory.Sender.Send(Arg.Any<CancelBookingByOfficeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        factory.Sender.Send(Arg.Any<ConfirmPaymentCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        factory.Sender.Send(Arg.Any<RejectPaymentCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var stops = (Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/office/trips/{Id}/bookings", new OfficeBookingRequest("Passenger", "01012345678", stops.Item1, stops.Item2, [1]))).StatusCode);
        await factory.Sender.Received().Send(Arg.Is<CreateOfficeBookingCommand>(x => x.TripId == Id && x.PassengerName == "Passenger" && x.SeatNumbers.SequenceEqual(new[] { 1 })), Arg.Any<CancellationToken>());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/office/trips/{Id}/assignment", new TripAssignmentRequest(Id, Id))).StatusCode);
        await factory.Sender.Received().Send(new UpdateTripAssignmentCommand(Id, Id, Id), Arg.Any<CancellationToken>());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/office/bookings/{Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/office/bookings/{Id}/payment/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/office/bookings/{Id}/payment/reject", new RejectPaymentRequest("Wrong amount"))).StatusCode);
        await factory.Sender.Received().Send(new RejectPaymentCommand(Id, "Wrong amount"), Arg.Any<CancellationToken>());
        var invalid = await AccountEndpointTests.Problem(await client.PostAsJsonAsync($"/api/v1/office/bookings/{Id}/payment/reject", new RejectPaymentRequest(string.Empty)), 400, ErrorCodes.ValidationFailed);
        Assert.True(invalid.GetProperty("errors").TryGetProperty("reason", out _));
        await AccountEndpointTests.Problem(await client.PutAsJsonAsync($"/api/v1/office/trips/{Id}/assignment", new TripAssignmentRequest(Guid.Empty, Id)), 400, ErrorCodes.ValidationFailed);
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync($"/api/v1/office/trips/{Id}/bookings", new OfficeBookingRequest(string.Empty, "invalid", stops.Item1, stops.Item2, [])), 400, ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Payments_PaginateAndReceiptStreamsRemainPrivate()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(new GetPendingPaymentsQuery(new QueryPage(2, 1)), Arg.Any<CancellationToken>()).Returns((Result<List<PendingPaymentDto>>)new List<PendingPaymentDto> { Payment(), Payment() });
        factory.Sender.Send(new GetReceiptQuery(Id), Arg.Any<CancellationToken>()).Returns((Result<ReceiptFileDto>)new ReceiptFileDto(new MemoryStream([1, 2, 3]), "image/png", "internal.png"));
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var page = await client.GetFromJsonAsync<PagedResponse<PendingPaymentDto>>("/api/v1/office/payments?page=2&pageSize=1");
        Assert.Single(page!.Data);
        Assert.True(page.Pagination.HasMore);
        Assert.Equal(2, page.Pagination.Page);
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/office/payments?page=0"), 400, ErrorCodes.ValidationFailed);
        var receipt = await client.GetAsync($"/api/v1/office/bookings/{Id}/receipt");
        Assert.Equal(new byte[] { 1, 2, 3 }, await receipt.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/png", receipt.Content.Headers.ContentType!.MediaType);
        Assert.True(receipt.Headers.CacheControl!.NoStore);
        Assert.Contains("nosniff", receipt.Headers.GetValues("X-Content-Type-Options"));
    }

    [Theory]
    [InlineData("manifest", 404, ErrorCodes.TripNotFound)]
    [InlineData("receipt", 404, ErrorCodes.ReceiptNotFound)]
    [InlineData("assignment", 409, ErrorCodes.LayoutDoesNotCoverBookedSeats)]
    [InlineData("confirm", 404, ErrorCodes.BookingNotFound)]
    [InlineData("reject", 409, ErrorCodes.BookingInvalidTransition)]
    [InlineData("cancel", 500, ErrorCodes.InternalError)]
    public async Task OfficeErrors_UseStableProblems(string action, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(new GetTripManifestQuery(Id), Arg.Any<CancellationToken>()).Returns((Result<TripManifestDto>)ApplicationErrors.TripNotFound);
        factory.Sender.Send(new GetReceiptQuery(Id), Arg.Any<CancellationToken>()).Returns((Result<ReceiptFileDto>)ApplicationErrors.ReceiptNotFound);
        factory.Sender.Send(Arg.Any<UpdateTripAssignmentCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)ApplicationErrors.LayoutDoesNotCoverBookedSeats([1]));
        factory.Sender.Send(new ConfirmPaymentCommand(Id), Arg.Any<CancellationToken>()).Returns((Result<Updated>)ApplicationErrors.BookingNotFound);
        factory.Sender.Send(Arg.Any<RejectPaymentCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)SBus.Domain.Bookings.BookingErrors.InvalidTransition(default, default));
        factory.Sender.Send(new CancelBookingByOfficeCommand(Id), Arg.Any<CancellationToken>()).Returns<Result<Updated>>(_ => throw new InvalidOperationException("secret"));
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var response = action switch
        {
            "manifest" => await client.GetAsync($"/api/v1/office/trips/{Id}/manifest"),
            "receipt" => await client.GetAsync($"/api/v1/office/bookings/{Id}/receipt"),
            "assignment" => await client.PutAsJsonAsync($"/api/v1/office/trips/{Id}/assignment", new TripAssignmentRequest(Id, Id)),
            "confirm" => await client.PostAsync($"/api/v1/office/bookings/{Id}/payment/confirm", null),
            "reject" => await client.PostAsJsonAsync($"/api/v1/office/bookings/{Id}/payment/reject", new RejectPaymentRequest("Reason")),
            _ => await client.PostAsync($"/api/v1/office/bookings/{Id}/cancel", null),
        };
        await AccountEndpointTests.Problem(response, status, code);
    }

    [Theory]
    [InlineData("book", "TripNotFound", 404, ErrorCodes.TripNotFound)]
    [InlineData("book", "TripDeparted", 409, ErrorCodes.TripDeparted)]
    [InlineData("book", "SeatsJustTaken", 409, ErrorCodes.BookingSeatUnavailable)]
    [InlineData("book", "StopsNotOnRoute", 400, ErrorCodes.BookingStopsNotOnRoute)]
    [InlineData("book", "PickupAfterDropoff", 400, ErrorCodes.BookingStopOrderInvalid)]
    [InlineData("book", "SeatsNotInLayout", 400, ErrorCodes.BookingSeatsNotInLayout)]
    [InlineData("assign", "TripNotFound", 404, ErrorCodes.TripNotFound)]
    [InlineData("assign", "BusNotFound", 404, ErrorCodes.BusNotFound)]
    [InlineData("assign", "DriverNotFound", 404, ErrorCodes.DriverNotFound)]
    [InlineData("assign", "BusInactive", 409, ErrorCodes.BusInactive)]
    [InlineData("assign", "DriverInactive", 409, ErrorCodes.DriverInactive)]
    [InlineData("assign", "TripDeparted", 409, ErrorCodes.TripDeparted)]
    [InlineData("cancel", "BookingNotFound", 404, ErrorCodes.BookingNotFound)]
    [InlineData("cancel", "InvalidTransition", 409, ErrorCodes.BookingInvalidTransition)]
    [InlineData("confirm", "InvalidTransition", 409, ErrorCodes.BookingInvalidTransition)]
    [InlineData("reject", "BookingNotFound", 404, ErrorCodes.BookingNotFound)]
    public async Task OfficeBusinessErrors_MatchSharedHandlers(string action, string name, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        var error = name switch
        {
            "SeatsNotInLayout" => ApplicationErrors.SeatsNotInLayout([1]),
            "InvalidTransition" => SBus.Domain.Bookings.BookingErrors.InvalidTransition(default, default),
            _ => (Error)typeof(ApplicationErrors).GetProperty(name)!.GetValue(null)!,
        };
        factory.Sender.Send(Arg.Any<CreateOfficeBookingCommand>(), Arg.Any<CancellationToken>()).Returns((Result<BookingCreatedDto>)error);
        factory.Sender.Send(Arg.Any<UpdateTripAssignmentCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        factory.Sender.Send(Arg.Any<CancelBookingByOfficeCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        factory.Sender.Send(Arg.Any<ConfirmPaymentCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        factory.Sender.Send(Arg.Any<RejectPaymentCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var response = action switch
        {
            "book" => await client.PostAsJsonAsync($"/api/v1/office/trips/{Id}/bookings", new OfficeBookingRequest("Name", "01012345678", Id, Guid.NewGuid(), [1])),
            "assign" => await client.PutAsJsonAsync($"/api/v1/office/trips/{Id}/assignment", new TripAssignmentRequest(Id, Id)),
            "reject" => await client.PostAsJsonAsync($"/api/v1/office/bookings/{Id}/payment/reject", new RejectPaymentRequest("Reason")),
            _ => await client.PostAsync($"/api/v1/office/bookings/{Id}/" + (action == "confirm" ? "payment/confirm" : "cancel"), null),
        };
        await AccountEndpointTests.Problem(response, status, code);
    }

    [Fact]
    public async Task Dashboard_RejectsInvalidDateAndDefaultsToCairoToday()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<GetOfficeDayQuery>(), Arg.Any<CancellationToken>()).Returns((Result<List<OfficeTripDto>>)new List<OfficeTripDto>());
        factory.Sender.Send(new GetPendingPaymentsQuery(), Arg.Any<CancellationToken>()).Returns((Result<List<PendingPaymentDto>>)new List<PendingPaymentDto>());
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/office/day?date=invalid"), 400, ErrorCodes.ValidationFailed);
        var result = (await client.GetFromJsonAsync<ApiResponse<OfficeDayResponse>>("/api/v1/office/day"))!.Data;
        Assert.Equal(result.Today, result.Date);
        await factory.Sender.Received().Send(new GetOfficeDayQuery(result.Today), Arg.Any<CancellationToken>());
    }

    private static PendingPaymentDto Payment() => new(Guid.NewGuid(), Id, "Name", "01012345678", [1], 150, Direction.CairoToSuez, new DateOnly(2030, 1, 2), new TimeOnly(9, 0), DateTimeOffset.UtcNow);
}