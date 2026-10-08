using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Validation;
using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

public class CreateOnlineBookingCommandHandler(
    ILogger<CreateOnlineBookingCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options)
    : IRequestHandler<CreateOnlineBookingCommand, Result<BookingCreatedDto>>
{
    private readonly ILogger<CreateOnlineBookingCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<BookingCreatedDto>> Handle(CreateOnlineBookingCommand command, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var prepared = await BookingPlacement.PrepareAsync(_context, command, nowUtc, requireActiveSchedule: true, ct);

        if (prepared.IsError)
        {
            return prepared.Errors;
        }

        var booking = Booking.CreateHold(
            Guid.CreateVersion7(),
            command.TripId,
            PublicToken.Create(),
            command.PassengerName,
            EgyptianPhone.Normalize(command.PhoneNumber)!,
            command.PickupStopId,
            command.DropoffStopId,
            command.SeatNumbers,
            prepared.Value.Price,
            nowUtc,
            _options.HoldDuration);

        if (booking.IsError)
        {
            return booking.Errors;
        }

        var saved = await BookingPlacement.SaveAsync(_context, booking.Value, ct);

        if (saved.IsSuccess)
        {
            _logger.LogInformation("Online hold created. Booking: {BookingId}, Trip: {TripId}, Seats: {SeatCount}", booking.Value.Id, command.TripId, command.SeatNumbers.Count);
        }

        return saved;
    }
}
