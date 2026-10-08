using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetOfficeDay;

public class GetOfficeDayQueryHandler(IAppDbContext context)
    : IRequestHandler<GetOfficeDayQuery, Result<List<OfficeTripDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<OfficeTripDto>>> Handle(GetOfficeDayQuery query, CancellationToken ct)
    {
        return await _context.Trips
            .AsNoTracking()
            .Where(t => t.ServiceDate == query.Date)
            .Where(t => _context.Schedules.Any(s => s.Id == t.ScheduleId && s.IsActive)
                || _context.BookingSeats.Any(s => s.TripId == t.Id && s.IsActive))
            .OrderBy(t => t.DepartureAtUtc)
            .Select(t => new OfficeTripDto(
                t.Id,
                t.Direction,
                t.DepartureTime,
                t.Bus!.PlateNumber,
                t.Driver!.Name,
                t.Bus.SeatLayout!.Seats.Count(),
                _context.Bookings.Where(b => b.TripId == t.Id && b.Status == BookingStatus.Confirmed).SelectMany(b => b.Seats).Count(),
                _context.Bookings.Where(b => b.TripId == t.Id && b.Status == BookingStatus.AwaitingConfirmation).SelectMany(b => b.Seats).Count(),
                _context.Bookings.Where(b => b.TripId == t.Id && b.Status == BookingStatus.HoldPendingPayment).SelectMany(b => b.Seats).Count()))
            .ToListAsync(ct);
    }
}
