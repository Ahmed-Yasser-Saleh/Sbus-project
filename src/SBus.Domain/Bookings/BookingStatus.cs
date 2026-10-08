namespace SBus.Domain.Bookings;

public enum BookingStatus
{
    HoldPendingPayment = 1,
    AwaitingConfirmation = 2,
    Confirmed = 3,
    Expired = 4,
    Rejected = 5,
    Cancelled = 6,
}
