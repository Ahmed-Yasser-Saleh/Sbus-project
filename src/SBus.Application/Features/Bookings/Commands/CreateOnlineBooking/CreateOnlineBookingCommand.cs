using MediatR;

using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

public sealed record CreateOnlineBookingCommand(
    Guid TripId,
    string PassengerName,
    string PhoneNumber,
    Guid PickupStopId,
    Guid DropoffStopId,
    List<int> SeatNumbers) : IRequest<Result<BookingCreatedDto>>, IBookingRequest;
