using MediatR;

using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Common.Validation;
using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;

public class CreateOfficeBookingCommandHandler(
    ILogger<CreateOfficeBookingCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<CreateOfficeBookingCommand, Result<BookingCreatedDto>>
{
    private readonly ILogger<CreateOfficeBookingCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<BookingCreatedDto>> Handle(CreateOfficeBookingCommand command, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var prepared = await BookingPlacement.PrepareAsync(_context, command, nowUtc, requireActiveSchedule: false, ct);

        if (prepared.IsError)
        {
            return prepared.Errors;
        }

        var booking = Booking.CreateByOffice(
            Guid.CreateVersion7(),
            command.TripId,
            PublicToken.Create(),
            command.PassengerName,
            EgyptianPhone.Normalize(command.PhoneNumber)!,
            command.PickupStopId,
            command.DropoffStopId,
            command.SeatNumbers,
            prepared.Value.Price,
            nowUtc);

        if (booking.IsError)
        {
            return booking.Errors;
        }

        var saved = await BookingPlacement.SaveAsync(_context, booking.Value, ct);

        if (saved.IsSuccess)
        {
            _logger.LogInformation("Office booking created. Booking: {BookingId}, Trip: {TripId}, Seats: {SeatCount}", booking.Value.Id, command.TripId, command.SeatNumbers.Count);
        }

        return saved;
    }
}
