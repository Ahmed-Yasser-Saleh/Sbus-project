using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Settings;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetTicket;

public class GetTicketQueryHandler(
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options)
    : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<TicketDto>> Handle(GetTicketQuery query, CancellationToken ct)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Seats)
            .Include(b => b.Trip!.Driver)
            .FirstOrDefaultAsync(b => b.PublicToken == query.PublicToken, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var trip = booking.Trip!;
        var nowUtc = _timeProvider.GetUtcNow();

        var stopNames = await _context.Stops
            .AsNoTracking()
            .Where(s => s.Id == booking.PickupStopId || s.Id == booking.DropoffStopId)
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var pickupMinutes = await _context.RouteStops
            .AsNoTracking()
            .Where(r => r.Direction == trip.Direction && r.StopId == booking.PickupStopId)
            .Select(r => (int?)r.MinutesFromStart)
            .FirstOrDefaultAsync(ct);

        var cancellationDeadline = trip.DepartureAtUtc - _options.PassengerCancellationCutoff;
        var isConfirmed = booking.Status == BookingStatus.Confirmed;
        var canCancel = booking.Status == BookingStatus.HoldPendingPayment
            || (booking.HoldsSeats && nowUtc <= cancellationDeadline);

        return new TicketDto(
            booking.Id,
            booking.PublicToken,
            booking.Status,
            booking.Source,
            booking.PassengerName,
            booking.PhoneNumber,
            booking.SeatNumbers,
            booking.PricePerSeat,
            booking.Amount,
            trip.Direction,
            trip.ServiceDate,
            trip.DepartureTime,
            stopNames.GetValueOrDefault(booking.PickupStopId, "—"),
            pickupMinutes is int minutes ? trip.DepartureTime.AddMinutes(minutes) : null,
            stopNames.GetValueOrDefault(booking.DropoffStopId, "—"),
            booking.HoldExpiresAtUtc,
            isConfirmed ? trip.Driver!.Name : null,
            isConfirmed ? trip.Driver!.PhoneNumber : null,
            booking.RejectionReason,
            booking.Status == BookingStatus.HoldPendingPayment,
            canCancel && !trip.HasDeparted(nowUtc),
            cancellationDeadline,
            trip.HasDeparted(nowUtc),
            _options.InstaPayAddress,
            _options.OfficePhone);
    }
}
