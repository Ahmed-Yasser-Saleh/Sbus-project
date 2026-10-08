using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Mappers;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetTripForBooking;

public class GetTripForBookingQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetTripForBookingQuery, Result<TripBookingDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<TripBookingDto>> Handle(GetTripForBookingQuery query, CancellationToken ct)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Bus!.SeatLayout!.Seats)
            .FirstOrDefaultAsync(t => t.Id == query.TripId, ct);

        if (trip is null)
        {
            return ApplicationErrors.TripNotFound;
        }

        var taken = await _context.BookingSeats
            .Where(s => s.TripId == trip.Id && s.IsActive)
            .Select(s => s.SeatNumber)
            .ToListAsync(ct);

        var stops = await GetRouteQueryHandler.LoadAsync(_context, trip.Direction, ct);

        return new TripBookingDto(
            trip.Id,
            trip.Direction,
            trip.ServiceDate,
            trip.DepartureTime,
            trip.Price,
            !trip.HasDeparted(_timeProvider.GetUtcNow()),
            trip.Bus!.SeatLayout!.ToSeatMap(taken.ToHashSet()),
            stops);
    }
}
