using System.Net;
using System.Net.Http.Json;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;
using SBus.Application.Features.Bookings.Commands.SubmitReceipt;
using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetMyTickets;
using SBus.Application.Features.Bookings.Queries.GetTicket;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

using Xunit;

namespace SBus.Api.Tests;

public sealed class TicketEndpointTests
{
    [Theory]
    [InlineData(BookingStatus.HoldPendingPayment)]
    [InlineData(BookingStatus.AwaitingConfirmation)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Expired)]
    public async Task Get_PreservesEveryStatusAndLegacyAnonymousAccess(BookingStatus status)
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<GetTicketQuery>(), Arg.Any<CancellationToken>()).Returns(new TicketDto(Guid.NewGuid(), "token", status, BookingSource.Online,
            "Name", "phone", [1], 150, 150, Direction.CairoToSuez, new(2030, 1, 1), new(9, 0), "Start", null, "End", null, null, null, null,
            false, false, DateTimeOffset.UtcNow, false, "address", "office"));
        using var client = factory.Client();
        var response = await client.GetAsync("/api/v1/tickets/token");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("cancel")]
    [InlineData("receipt")]
    public async Task TicketOperations_HideMissingAndInaccessibleBookings(string action)
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<GetTicketQuery>(), Arg.Any<CancellationToken>()).Returns(ApplicationErrors.BookingNotFound);
        factory.Sender.Send(Arg.Any<CancelBookingByPassengerCommand>(), Arg.Any<CancellationToken>()).Returns(ApplicationErrors.BookingNotFound);
        factory.Sender.Send(Arg.Any<SubmitReceiptCommand>(), Arg.Any<CancellationToken>()).Returns(ApplicationErrors.BookingNotFound);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await Send(client, action), 404, ErrorCodes.BookingNotFound);
    }

    [Theory]
    [InlineData("cancel", 0, 409, "BOOKING_INVALID_TRANSITION")]
    [InlineData("cancel", 1, 409, "BOOKING_CANCELLATION_CLOSED")]
    [InlineData("receipt", 2, 409, "BOOKING_RECEIPT_NOT_ALLOWED")]
    [InlineData("receipt", 3, 400, "RECEIPT_TOO_LARGE")]
    [InlineData("receipt", 4, 400, "RECEIPT_UNSUPPORTED_FILE")]
    public async Task TicketMutations_MapBusinessFailures(string action, int scenario, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        var error = scenario switch
        {
            0 => BookingErrors.InvalidTransition(BookingStatus.Expired, BookingStatus.Cancelled),
            1 => BookingErrors.PassengerCancellationClosed(TimeSpan.FromHours(2)),
            2 => BookingErrors.ReceiptNotAllowed(BookingStatus.Expired),
            3 => Error.Validation(ReceiptErrorKeys.TooLarge, "Too large."),
            _ => Error.Validation(ReceiptErrorKeys.UnsupportedFile, "Unsupported file."),
        };
        factory.Sender.Send(Arg.Any<CancelBookingByPassengerCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        factory.Sender.Send(Arg.Any<SubmitReceiptCommand>(), Arg.Any<CancellationToken>()).Returns((Result<Updated>)error);
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await Send(client, action), status, code);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("receipt")]
    public async Task TicketMutations_ReturnSuccessAndRateLimit(string action)
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<CancelBookingByPassengerCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        factory.Sender.Send(Arg.Any<SubmitReceiptCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Updated);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await Send(client, action)).StatusCode);
        for (var index = 0; index < 9; index++)
        {
            await Send(client, action);
        }

        var response = await Send(client, action);
        await AccountEndpointTests.Problem(response, 429, ErrorCodes.RateLimitExceeded);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Receipt_MissingFileBindsFieldError()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("no-file"), "notes");
        var problem = await AccountEndpointTests.Problem(await client.PostAsync("/api/v1/tickets/token/receipt", form), 400, ErrorCodes.ValidationFailed);
        Assert.True(problem.GetProperty("errors").TryGetProperty("receipt", out _));
    }

    [Fact]
    public async Task MyTickets_PreservesTwentyItemPageAndQueriesCurrentUser()
    {
        await using var factory = new ApiTestFactory();
        factory.Sender.Send(Arg.Any<GetMyTicketsQuery>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<MyTicketDto>());
        using var client = BookingEndpointTests.Traveler(factory);
        var response = await client.GetAsync("/api/v1/me/tickets?page=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await factory.Sender.Received().Send(new GetMyTicketsQuery(2), Arg.Any<CancellationToken>());
        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/me/tickets?page=0"), 400, ErrorCodes.ValidationFailed);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("CompanyOwner", 403)]
    public async Task MyTickets_EnforcesAuthenticationAndRoles(string? role, int status)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        if (role is not null)
        {
            factory.Authenticate(client, role);
        }

        await AccountEndpointTests.Problem(await client.GetAsync("/api/v1/me/tickets"), status, status == 401 ? ErrorCodes.Unauthenticated : ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Receipt_UnsupportedMediaAndTransportLimitUseProblems()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await client.PostAsJsonAsync("/api/v1/tickets/token/receipt", new { receipt = "invalid" }), 415, ErrorCodes.UnsupportedMediaType);
        factory.Sender.Send(Arg.Any<SubmitReceiptCommand>(), Arg.Any<CancellationToken>()).Returns<Result<Updated>>(
            _ => throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Transport body limit exceeded.", 413));
        await AccountEndpointTests.Problem(await Send(client, "receipt"), 413, ErrorCodes.RequestTooLarge);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string action)
    {
        if (action == "get")
        {
            return client.GetAsync("/api/v1/tickets/token");
        }

        if (action == "cancel")
        {
            return client.PostAsync("/api/v1/tickets/token/cancel", null);
        }

        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([0x89, 0x50, 0x4e, 0x47]), "receipt", "receipt.png");
        return client.PostAsync("/api/v1/tickets/token/receipt", form);
    }
}