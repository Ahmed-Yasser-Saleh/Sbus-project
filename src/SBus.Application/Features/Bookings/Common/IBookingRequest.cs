namespace SBus.Application.Features.Bookings.Common;

public interface IBookingRequest
{
    Guid TripId { get; }
    string PassengerName { get; }
    string PhoneNumber { get; }
    Guid PickupStopId { get; }
    Guid DropoffStopId { get; }
    List<int> SeatNumbers { get; }
}
