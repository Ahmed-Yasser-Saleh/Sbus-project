using SBus.Domain.Bookings;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;
using SBus.Tests.Common.Bookings;

using Xunit;

namespace SBus.Domain.UnitTests.Bookings;

public class BookingTests
{
    private static readonly DateTimeOffset Now = BookingFactory.Now;

    [Fact]
    public void CreateHold_ValidInput_HoldsSeatsUntilExpiry()
    {
        var result = BookingFactory.CreateHold(seats: [3, 1], pricePerSeat: 120);

        Assert.True(result.IsSuccess);
        var booking = result.Value;
        Assert.Equal(BookingStatus.HoldPendingPayment, booking.Status);
        Assert.Equal(BookingSource.Online, booking.Source);
        Assert.Equal(Now.AddMinutes(15), booking.HoldExpiresAtUtc);
        Assert.Equal([1, 3], booking.SeatNumbers);
        Assert.Equal(240, booking.Amount);
        Assert.True(booking.HoldsSeats);
        Assert.All(booking.Seats, s => Assert.True(s.IsActive));
    }

    [Fact]
    public void CreateHold_DuplicateSeats_Fails()
    {
        var result = BookingFactory.CreateHold(seats: [2, 2]);

        Assert.Equal(BookingErrors.DuplicateSeats.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateHold_TooManySeats_Fails()
    {
        var seats = Enumerable.Range(1, SBusConstants.MaxSeatsPerBooking + 1).ToList();

        var result = BookingFactory.CreateHold(seats: seats);

        Assert.Equal(BookingErrors.TooManySeats.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateHold_SamePickupAndDropoff_Fails()
    {
        var stop = Guid.CreateVersion7();

        var result = BookingFactory.CreateHold(pickupStopId: stop, dropoffStopId: stop);

        Assert.Equal(BookingErrors.SameStop.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateHold_NoSeats_Fails()
    {
        var result = BookingFactory.CreateHold(seats: []);

        Assert.Equal(BookingErrors.SeatsRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateByOffice_IsConfirmedImmediately()
    {
        var booking = BookingFactory.CreateByOffice().Value;

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(BookingSource.Office, booking.Source);
        Assert.Null(booking.HoldExpiresAtUtc);
        Assert.Equal(Now, booking.ConfirmedAtUtc);
    }

    [Fact]
    public void SubmitReceipt_FromHold_MovesToAwaitingConfirmation()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.SubmitReceipt("abc.png", Now.AddMinutes(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.AwaitingConfirmation, booking.Status);
        Assert.Equal("abc.png", booking.ReceiptFileName);
        Assert.True(booking.HoldsSeats);
    }

    [Fact]
    public void SubmitReceipt_Twice_Fails()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now);

        var result = booking.SubmitReceipt("def.png", Now);

        Assert.Equal("BookingErrors.ReceiptNotAllowed", result.TopError.Code);
    }

    [Fact]
    public void SubmitReceipt_AfterExpiryTimeButBeforeCleanup_IsStillAccepted()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.SubmitReceipt("abc.png", Now.AddMinutes(16));

        Assert.True(result.IsSuccess);
        Assert.False(booking.IsHoldOverdue(Now.AddMinutes(16)));
    }

    [Fact]
    public void Confirm_FromAwaiting_Succeeds()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now);

        var result = booking.Confirm(Now.AddMinutes(10));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Confirm_WithoutReceipt_Fails()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.Confirm(Now);

        Assert.Equal("BookingErrors.InvalidTransition", result.TopError.Code);
        Assert.Equal(BookingStatus.HoldPendingPayment, booking.Status);
    }

    [Fact]
    public void Reject_ReleasesSeats()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now);

        var result = booking.Reject("المبلغ ناقص", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.False(booking.HoldsSeats);
        Assert.All(booking.Seats, s => Assert.False(s.IsActive));
    }

    [Fact]
    public void Reject_WithoutReason_Fails()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now);

        var result = booking.Reject("  ", Now);

        Assert.Equal(BookingErrors.RejectionReasonRequired.Code, result.TopError.Code);
        Assert.Equal(BookingStatus.AwaitingConfirmation, booking.Status);
    }

    [Fact]
    public void Expire_BeforeHoldEnds_Fails()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.Expire(Now.AddMinutes(14));

        Assert.Equal(BookingErrors.HoldNotOverdue.Code, result.TopError.Code);
        Assert.True(booking.HoldsSeats);
    }

    [Fact]
    public void Expire_AfterHoldEnds_ReleasesSeats()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.Expire(Now.AddMinutes(15));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Expired, booking.Status);
        Assert.All(booking.Seats, s => Assert.False(s.IsActive));
    }

    [Fact]
    public void Expire_AfterReceiptSubmitted_Fails()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now.AddMinutes(1));

        var result = booking.Expire(Now.AddHours(1));

        Assert.Equal("BookingErrors.InvalidTransition", result.TopError.Code);
        Assert.Equal(BookingStatus.AwaitingConfirmation, booking.Status);
    }

    [Fact]
    public void CancelByPassenger_ConfirmedBeforeCutoff_Succeeds()
    {
        var booking = ConfirmedBooking();
        var departure = Now.AddHours(5);

        var result = booking.CancelByPassenger(Now.AddHours(2), departure, TimeSpan.FromHours(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(CancelledBy.Passenger, booking.CancelledBy);
        Assert.All(booking.Seats, s => Assert.False(s.IsActive));
    }

    [Fact]
    public void CancelByPassenger_ConfirmedAfterCutoff_Fails()
    {
        var booking = ConfirmedBooking();
        var departure = Now.AddHours(5);

        var result = booking.CancelByPassenger(Now.AddHours(3).AddMinutes(1), departure, TimeSpan.FromHours(2));

        Assert.Equal("BookingErrors.PassengerCancellationClosed", result.TopError.Code);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void CancelByPassenger_UnpaidHold_IsAllowedEvenAfterCutoff()
    {
        var booking = BookingFactory.CreateHold().Value;

        var result = booking.CancelByPassenger(Now.AddMinutes(1), Now.AddMinutes(30), TimeSpan.FromHours(2));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CancelByOffice_AlreadyCancelled_Fails()
    {
        var booking = ConfirmedBooking();
        booking.CancelByOffice(Now);

        var result = booking.CancelByOffice(Now);

        Assert.Equal("BookingErrors.InvalidTransition", result.TopError.Code);
    }

    private static Booking ConfirmedBooking()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.SubmitReceipt("abc.png", Now);
        booking.Confirm(Now);
        return booking;
    }
}
