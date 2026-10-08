using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SBus.Application.Common.Errors;
using SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;
using SBus.Application.Features.Bookings.Commands.ConfirmPayment;
using SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Commands.ExpireOverdueHolds;
using SBus.Application.Features.Bookings.Commands.RejectPayment;
using SBus.Application.Features.Bookings.Commands.SubmitReceipt;
using SBus.Application.Features.Bookings.Queries.GetTicket;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Application.SubcutaneousTests.Common;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Bookings;

[Collection(WebAppFactoryCollection.Name)]
public class BookingFlowTests(WebAppFactory factory)
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];

    private readonly WebAppFactory _factory = factory;

    [DatabaseFact]
    public async Task OnlineBooking_HoldsTheSeat_AndShowsItTakenOnTheMap()
    {
        var tripId = await NewTripAsync();

        var created = await Send(Online(tripId, [3, 4]));

        Assert.True(created.IsSuccess);
        Assert.Equal(32, created.Value.PublicToken.Length);

        var map = await Send(new GetTripForBookingQuery(tripId));
        var taken = map.Value.SeatMap.Cells.Where(c => c.IsTaken && c.SeatNumber is not null).Select(c => c.SeatNumber!.Value).Order();
        Assert.Equal([3, 4], taken);
    }

    [DatabaseFact]
    public async Task SameSeatTwice_SecondBookingIsRefused()
    {
        var tripId = await NewTripAsync();
        await Send(Online(tripId, [5]));

        var second = await Send(Online(tripId, [5, 6]));

        Assert.True(second.IsError);
        Assert.Equal(ApplicationErrors.SeatsTaken([5]).Code, second.TopError.Code);
    }

    [DatabaseFact]
    public async Task TwentyConcurrentBookingsOfOneSeat_ExactlyOneSucceeds()
    {
        var tripId = await NewTripAsync();

        var attempts = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => Send(Online(tripId, [7]))))
            .ToList();

        var results = await Task.WhenAll(attempts);

        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => r.IsError), r => Assert.Equal(ErrorKind.Conflict, r.TopError.Type));

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.BookingSeats.CountAsync(s => s.TripId == tripId && s.SeatNumber == 7 && s.IsActive));
    }

    [DatabaseFact]
    public async Task ExpiredHold_FreesTheSeatForTheNextPassenger()
    {
        var tripId = await NewTripAsync();
        var first = await Send(Online(tripId, [8]));

        _factory.Clock.Advance(TimeSpan.FromMinutes(16));

        var second = await Send(Online(tripId, [8]));

        Assert.True(second.IsSuccess);
        var firstTicket = await Send(new GetTicketQuery(first.Value.PublicToken));
        Assert.Equal(BookingStatus.Expired, firstTicket.Value.Status);
    }

    [DatabaseFact]
    public async Task ExpireOverdueHolds_ExpiresOnlyUnpaidHoldsPastTheirTime()
    {
        var tripId = await NewTripAsync();
        var unpaid = await Send(Online(tripId, [9]));
        var paid = await Send(Online(tripId, [10]));
        await SubmitReceipt(paid.Value.PublicToken);

        _factory.Clock.Advance(TimeSpan.FromMinutes(16));
        await Send(new ExpireOverdueHoldsCommand());

        Assert.Equal(BookingStatus.Expired, (await Send(new GetTicketQuery(unpaid.Value.PublicToken))).Value.Status);
        Assert.Equal(BookingStatus.AwaitingConfirmation, (await Send(new GetTicketQuery(paid.Value.PublicToken))).Value.Status);
    }

    [DatabaseFact]
    public async Task DriverPhone_IsHiddenUntilThePaymentIsConfirmed()
    {
        var tripId = await NewTripAsync();
        var created = await Send(Online(tripId, [11]));
        var token = created.Value.PublicToken;

        await SubmitReceipt(token);
        var beforeConfirm = await Send(new GetTicketQuery(token));

        Assert.Equal(BookingStatus.AwaitingConfirmation, beforeConfirm.Value.Status);
        Assert.Null(beforeConfirm.Value.DriverPhoneNumber);

        await Send(new ConfirmPaymentCommand(created.Value.BookingId));
        var afterConfirm = await Send(new GetTicketQuery(token));

        Assert.Equal(BookingStatus.Confirmed, afterConfirm.Value.Status);
        Assert.Equal(TestWorld.DriverPhone, afterConfirm.Value.DriverPhoneNumber);
    }

    [DatabaseFact]
    public async Task ReceiptThatIsNotAnImage_IsRefused()
    {
        var tripId = await NewTripAsync();
        var created = await Send(Online(tripId, [12]));

        using var html = new MemoryStream("<html><script>alert(1)</script>"u8.ToArray());
        var result = await Send(new SubmitReceiptCommand(created.Value.PublicToken, html, html.Length));

        Assert.True(result.IsError);
        Assert.Equal(BookingStatus.HoldPendingPayment, (await Send(new GetTicketQuery(created.Value.PublicToken))).Value.Status);
    }

    [DatabaseFact]
    public async Task RejectedPayment_FreesTheSeats()
    {
        var tripId = await NewTripAsync();
        var created = await Send(Online(tripId, [13]));
        await SubmitReceipt(created.Value.PublicToken);

        var rejected = await Send(new RejectPaymentCommand(created.Value.BookingId, "مفيش تحويل وصل"));
        var rebook = await Send(Online(tripId, [13]));

        Assert.True(rejected.IsSuccess);
        Assert.True(rebook.IsSuccess);
    }

    [DatabaseFact]
    public async Task PassengerCancellation_IsClosedTwoHoursBeforeDeparture()
    {
        var tripId = await NewTripAsync(departsIn: TimeSpan.FromHours(3));
        var booking = await Send(Office(tripId, [14]));

        _factory.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        var result = await Send(new CancelBookingByPassengerCommand(booking.Value.PublicToken));

        Assert.True(result.IsError);
        Assert.Equal("BookingErrors.PassengerCancellationClosed", result.TopError.Code);
    }

    [DatabaseFact]
    public async Task PassengerCancellation_BeforeCutoff_FreesTheSeat()
    {
        var tripId = await NewTripAsync();
        var booking = await Send(Office(tripId, [1]));

        var cancelled = await Send(new CancelBookingByPassengerCommand(booking.Value.PublicToken));
        var rebook = await Send(Online(tripId, [1]));

        Assert.True(cancelled.IsSuccess);
        Assert.True(rebook.IsSuccess);
    }

    [DatabaseFact]
    public async Task SeatNotInTheBus_IsRefused()
    {
        var tripId = await NewTripAsync();

        var result = await Send(Online(tripId, [99]));

        Assert.Equal(ApplicationErrors.SeatsNotInLayout([99]).Code, result.TopError.Code);
    }

    [DatabaseFact]
    public async Task PickupAfterDropoff_IsRefused()
    {
        var tripId = await NewTripAsync();
        var world = _factory.World;

        var result = await Send(new CreateOnlineBookingCommand(tripId, "Mona", "01012345678", world.LastStopId, world.FirstStopId, [2]));

        Assert.Equal(ApplicationErrors.PickupAfterDropoff.Code, result.TopError.Code);
    }

    [DatabaseFact]
    public async Task DepartedTrip_CannotBeBooked()
    {
        var tripId = await NewTripAsync(departsIn: TimeSpan.FromMinutes(30));
        _factory.Clock.Advance(TimeSpan.FromMinutes(31));

        var result = await Send(Online(tripId, [2]));

        Assert.Equal(ApplicationErrors.TripDeparted.Code, result.TopError.Code);
    }

    private async Task<Guid> NewTripAsync(TimeSpan? departsIn = null)
    {
        await using var db = _factory.CreateDbContext();
        return await _factory.World.CreateTripAsync(db, _factory.Clock.GetUtcNow().Add(departsIn ?? TimeSpan.FromDays(2)));
    }

    private CreateOnlineBookingCommand Online(Guid tripId, List<int> seats) =>
        new(tripId, "Mona", "01012345678", _factory.World.FirstStopId, _factory.World.LastStopId, seats);

    private CreateOfficeBookingCommand Office(Guid tripId, List<int> seats) =>
        new(tripId, "Karim", "01112345678", _factory.World.MiddleStopId, _factory.World.LastStopId, seats);

    private async Task SubmitReceipt(string token)
    {
        using var content = new MemoryStream(PngHeader);
        var result = await Send(new SubmitReceiptCommand(token, content, content.Length));
        Assert.True(result.IsSuccess, result.IsError ? result.TopError.Description : null);
    }

    private async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _factory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }
}
