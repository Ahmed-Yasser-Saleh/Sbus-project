using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Time;
using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetAvailableTrips;

public class GetAvailableTripsQueryHandler(
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options)
    : IRequestHandler<GetAvailableTripsQuery, Result<List<TripSummaryDto>>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<List<TripSummaryDto>>> Handle(GetAvailableTripsQuery query, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var today = CairoTime.Today(nowUtc);

        if (query.Date < today || query.Date >= today.AddDays(_options.BookingWindowDays))
        {
            return new List<TripSummaryDto>();
        }

        var trips = await _context.Trips
            .AsNoTracking()
            .Where(t => t.Direction == query.Direction && t.ServiceDate == query.Date && t.DepartureAtUtc > nowUtc)
            .Where(t => _context.Schedules.Any(s => s.Id == t.ScheduleId && s.IsActive))
            .OrderBy(t => t.DepartureAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Direction,
                t.ServiceDate,
                t.DepartureTime,
                t.Price,
                Capacity = t.Bus!.SeatLayout!.Seats.Count(),
                Taken = _context.BookingSeats.Count(s => s.TripId == t.Id && s.IsActive),
            })
            .ToListAsync(ct);

        return trips
            .Select(t => new TripSummaryDto(t.Id, t.Direction, t.ServiceDate, t.DepartureTime, t.Price, t.Capacity, Math.Max(0, t.Capacity - t.Taken)))
            .ToList();
    }
}
