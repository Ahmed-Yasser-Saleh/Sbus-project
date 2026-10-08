using SBus.Domain.Common;

namespace SBus.Domain.Bookings;

public sealed class BookingSeat : Entity
{
    public Guid BookingId { get; private set; }
    public Guid TripId { get; private set; }
    public int SeatNumber { get; private set; }
    public bool IsActive { get; private set; }

    internal BookingSeat(Guid id, Guid bookingId, Guid tripId, int seatNumber)
        : base(id)
    {
        BookingId = bookingId;
        TripId = tripId;
        SeatNumber = seatNumber;
        IsActive = true;
    }

    private BookingSeat()
    { }

    internal void Release()
    {
        IsActive = false;
    }
}
