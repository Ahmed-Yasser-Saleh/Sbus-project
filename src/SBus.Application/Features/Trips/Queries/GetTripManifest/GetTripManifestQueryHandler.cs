using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Mappers;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetTripManifest;

public class GetTripManifestQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetTripManifestQuery, Result<TripManifestDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<TripManifestDto>> Handle(GetTripManifestQuery query, CancellationToken ct)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Bus!.SeatLayout!.Seats)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == query.TripId, ct);

        if (trip is null)
        {
            return ApplicationErrors.TripNotFound;
        }

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Seats)
            .Where(b => b.TripId == trip.Id
                && (b.Status == BookingStatus.HoldPendingPayment
                    || b.Status == BookingStatus.AwaitingConfirmation
                    || b.Status == BookingStatus.Confirmed))
            .ToListAsync(ct);

        var stopIds = bookings.SelectMany(b => new[] { b.PickupStopId, b.DropoffStopId }).Distinct().ToList();

        var stopNames = await _context.Stops
            .AsNoTracking()
            .Where(s => stopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var route = await GetRouteQueryHandler.LoadAsync(_context, trip.Direction, ct);
        var order = route.ToDictionary(r => r.StopId, r => r.Order);

        var taken = bookings.SelectMany(b => b.SeatNumbers).ToHashSet();

        var manifest = bookings
            .Select(b => new ManifestBookingDto(
                b.Id,
                b.PassengerName,
                b.PhoneNumber,
                b.SeatNumbers,
                stopNames.GetValueOrDefault(b.PickupStopId, "—"),
                stopNames.GetValueOrDefault(b.DropoffStopId, "—"),
                order.GetValueOrDefault(b.PickupStopId, int.MaxValue),
                b.Status,
                b.Source,
                b.Amount))
            .OrderBy(b => b.PickupOrder)
            .ThenBy(b => b.Seats.FirstOrDefault())
            .ToList();

        return new TripManifestDto(
            trip.Id,
            trip.Direction,
            trip.ServiceDate,
            trip.DepartureTime,
            trip.Price,
            trip.BusId,
            trip.Bus!.PlateNumber,
            trip.DriverId,
            trip.Driver!.Name,
            trip.Driver.PhoneNumber,
            trip.HasDeparted(_timeProvider.GetUtcNow()),
            trip.Bus.SeatLayout!.ToSeatMap(taken),
            route,
            manifest);
    }
}
