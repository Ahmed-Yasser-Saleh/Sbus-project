using SBus.Domain.Bookings;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record ManifestBookingDto(
    Guid BookingId,
    string PassengerName,
    string PhoneNumber,
    IReadOnlyList<int> Seats,
    string PickupStopName,
    string DropoffStopName,
    int PickupOrder,
    BookingStatus Status,
    BookingSource Source,
    decimal Amount);
