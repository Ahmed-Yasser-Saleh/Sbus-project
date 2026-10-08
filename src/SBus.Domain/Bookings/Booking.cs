using SBus.Domain.Common;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;
using SBus.Domain.Trips;

namespace SBus.Domain.Bookings;

public sealed class Booking : AuditableEntity
{
    public const int RejectionReasonMaxLength = 200;

    public Guid TripId { get; private set; }
    public Trip? Trip { get; private set; }
    public string PublicToken { get; private set; } = null!;
    public string PassengerName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public Guid PickupStopId { get; private set; }
    public Guid DropoffStopId { get; private set; }
    public decimal PricePerSeat { get; private set; }
    public decimal Amount { get; private set; }
    public BookingStatus Status { get; private set; }
    public BookingSource Source { get; private set; }
    public DateTimeOffset? HoldExpiresAtUtc { get; private set; }
    public string? ReceiptFileName { get; private set; }
    public DateTimeOffset? ReceiptSubmittedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public CancelledBy? CancelledBy { get; private set; }
    public string? RejectionReason { get; private set; }

    private readonly List<BookingSeat> _seats = [];
    public IEnumerable<BookingSeat> Seats => _seats.AsReadOnly();

    public IReadOnlyList<int> SeatNumbers => [.. _seats.Select(s => s.SeatNumber).Order()];

    public bool HoldsSeats => Status is BookingStatus.HoldPendingPayment or BookingStatus.AwaitingConfirmation or BookingStatus.Confirmed;

    private Booking()
    { }

    private Booking(Guid id, Guid tripId, string publicToken, string passengerName, string phoneNumber, Guid pickupStopId, Guid dropoffStopId, decimal pricePerSeat, BookingSource source)
        : base(id)
    {
        TripId = tripId;
        PublicToken = publicToken;
        PassengerName = passengerName;
        PhoneNumber = phoneNumber;
        PickupStopId = pickupStopId;
        DropoffStopId = dropoffStopId;
        PricePerSeat = pricePerSeat;
        Source = source;
    }

    public static Result<Booking> CreateHold(
        Guid id,
        Guid tripId,
        string publicToken,
        string passengerName,
        string phoneNumber,
        Guid pickupStopId,
        Guid dropoffStopId,
        IReadOnlyCollection<int> seatNumbers,
        decimal pricePerSeat,
        DateTimeOffset nowUtc,
        TimeSpan holdDuration)
    {
        if (holdDuration <= TimeSpan.Zero)
        {
            return BookingErrors.HoldDurationInvalid;
        }

        var result = Create(id, tripId, publicToken, passengerName, phoneNumber, pickupStopId, dropoffStopId, seatNumbers, pricePerSeat, BookingSource.Online);

        if (result.IsError)
        {
            return result.Errors;
        }

        var booking = result.Value;
        booking.Status = BookingStatus.HoldPendingPayment;
        booking.HoldExpiresAtUtc = nowUtc.Add(holdDuration);

        return booking;
    }

    public static Result<Booking> CreateByOffice(
        Guid id,
        Guid tripId,
        string publicToken,
        string passengerName,
        string phoneNumber,
        Guid pickupStopId,
        Guid dropoffStopId,
        IReadOnlyCollection<int> seatNumbers,
        decimal pricePerSeat,
        DateTimeOffset nowUtc)
    {
        var result = Create(id, tripId, publicToken, passengerName, phoneNumber, pickupStopId, dropoffStopId, seatNumbers, pricePerSeat, BookingSource.Office);

        if (result.IsError)
        {
            return result.Errors;
        }

        var booking = result.Value;
        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAtUtc = nowUtc;

        return booking;
    }

    public bool IsHoldOverdue(DateTimeOffset nowUtc) =>
        Status == BookingStatus.HoldPendingPayment && HoldExpiresAtUtc <= nowUtc;

    public Result<Success> EnsureCanSubmitReceipt()
    {
        if (Status != BookingStatus.HoldPendingPayment)
        {
            return BookingErrors.ReceiptNotAllowed(Status);
        }

        return Result.Success;
    }

    public Result<Updated> SubmitReceipt(string receiptFileName, DateTimeOffset nowUtc)
    {
        var allowed = EnsureCanSubmitReceipt();

        if (allowed.IsError)
        {
            return allowed.Errors;
        }

        if (string.IsNullOrWhiteSpace(receiptFileName))
        {
            return BookingErrors.ReceiptRequired;
        }

        ReceiptFileName = receiptFileName;
        ReceiptSubmittedAtUtc = nowUtc;
        Status = BookingStatus.AwaitingConfirmation;

        return Result.Updated;
    }

    public Result<Updated> Confirm(DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.AwaitingConfirmation)
        {
            return BookingErrors.InvalidTransition(Status, BookingStatus.Confirmed);
        }

        Status = BookingStatus.Confirmed;
        ConfirmedAtUtc = nowUtc;

        return Result.Updated;
    }

    public Result<Updated> Reject(string reason, DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.AwaitingConfirmation)
        {
            return BookingErrors.InvalidTransition(Status, BookingStatus.Rejected);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return BookingErrors.RejectionReasonRequired;
        }

        if (reason.Trim().Length > RejectionReasonMaxLength)
        {
            return BookingErrors.RejectionReasonTooLong;
        }

        RejectionReason = reason.Trim();
        Close(BookingStatus.Rejected, nowUtc);

        return Result.Updated;
    }

    public Result<Updated> CancelByPassenger(DateTimeOffset nowUtc, DateTimeOffset departureAtUtc, TimeSpan cancellationCutoff)
    {
        if (!HoldsSeats)
        {
            return BookingErrors.InvalidTransition(Status, BookingStatus.Cancelled);
        }

        if (Status != BookingStatus.HoldPendingPayment && nowUtc > departureAtUtc - cancellationCutoff)
        {
            return BookingErrors.PassengerCancellationClosed(cancellationCutoff);
        }

        CancelledBy = Bookings.CancelledBy.Passenger;
        Close(BookingStatus.Cancelled, nowUtc);

        return Result.Updated;
    }

    public Result<Updated> CancelByOffice(DateTimeOffset nowUtc)
    {
        if (!HoldsSeats)
        {
            return BookingErrors.InvalidTransition(Status, BookingStatus.Cancelled);
        }

        CancelledBy = Bookings.CancelledBy.Office;
        Close(BookingStatus.Cancelled, nowUtc);

        return Result.Updated;
    }

    public Result<Updated> Expire(DateTimeOffset nowUtc)
    {
        if (Status != BookingStatus.HoldPendingPayment)
        {
            return BookingErrors.InvalidTransition(Status, BookingStatus.Expired);
        }

        if (!IsHoldOverdue(nowUtc))
        {
            return BookingErrors.HoldNotOverdue;
        }

        Close(BookingStatus.Expired, nowUtc);

        return Result.Updated;
    }

    private static Result<Booking> Create(
        Guid id,
        Guid tripId,
        string publicToken,
        string passengerName,
        string phoneNumber,
        Guid pickupStopId,
        Guid dropoffStopId,
        IReadOnlyCollection<int> seatNumbers,
        decimal pricePerSeat,
        BookingSource source)
    {
        if (tripId == Guid.Empty)
        {
            return BookingErrors.TripRequired;
        }

        if (string.IsNullOrWhiteSpace(publicToken))
        {
            return BookingErrors.PublicTokenRequired;
        }

        if (string.IsNullOrWhiteSpace(passengerName))
        {
            return BookingErrors.PassengerNameRequired;
        }

        if (passengerName.Trim().Length > SBusConstants.NameMaxLength)
        {
            return BookingErrors.PassengerNameTooLong;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BookingErrors.PhoneRequired;
        }

        if (phoneNumber.Trim().Length > SBusConstants.PhoneMaxLength)
        {
            return BookingErrors.PhoneTooLong;
        }

        if (pickupStopId == Guid.Empty || dropoffStopId == Guid.Empty)
        {
            return BookingErrors.StopsRequired;
        }

        if (pickupStopId == dropoffStopId)
        {
            return BookingErrors.SameStop;
        }

        if (seatNumbers is null || seatNumbers.Count == 0)
        {
            return BookingErrors.SeatsRequired;
        }

        if (seatNumbers.Count > SBusConstants.MaxSeatsPerBooking)
        {
            return BookingErrors.TooManySeats;
        }

        if (seatNumbers.Any(n => n <= 0))
        {
            return BookingErrors.SeatNumberInvalid;
        }

        if (seatNumbers.Distinct().Count() != seatNumbers.Count)
        {
            return BookingErrors.DuplicateSeats;
        }

        if (pricePerSeat <= 0)
        {
            return BookingErrors.PriceInvalid;
        }

        var booking = new Booking(id, tripId, publicToken, passengerName.Trim(), phoneNumber.Trim(), pickupStopId, dropoffStopId, pricePerSeat, source);

        foreach (var seatNumber in seatNumbers.Order())
        {
            booking._seats.Add(new BookingSeat(Guid.CreateVersion7(), booking.Id, tripId, seatNumber));
        }

        booking.Amount = pricePerSeat * seatNumbers.Count;

        return booking;
    }

    private void Close(BookingStatus status, DateTimeOffset nowUtc)
    {
        Status = status;
        ClosedAtUtc = nowUtc;

        foreach (var seat in _seats)
        {
            seat.Release();
        }
    }
}
