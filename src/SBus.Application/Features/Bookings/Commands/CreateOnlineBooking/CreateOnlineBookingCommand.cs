using MediatR;

using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

public sealed record CreateOnlineBookingCommand(
    Guid TripId,
    Guid PickupStopId,
    Guid DropoffStopId,
    List<int> SeatNumbers,
    string? PassengerName = null,
    string? PhoneNumber = null,
    bool UpdateAccountDetails = false) : IRequest<Result<BookingCreatedDto>>, IBookingRequest
{
    public override string ToString() => nameof(CreateOnlineBookingCommand);
}
