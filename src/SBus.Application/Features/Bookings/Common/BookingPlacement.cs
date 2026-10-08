using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Persistence;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;
using SBus.Domain.Trips;

namespace SBus.Application.Features.Bookings.Common;

internal static class BookingPlacement
{
    public static async Task<Result<Trip>> PrepareAsync(IAppDbContext context, IBookingRequest request, DateTimeOffset nowUtc, bool requireActiveSchedule, CancellationToken ct)
    {
        var trip = await context.Trips
            .Include(t => t.Bus!.SeatLayout!.Seats)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return ApplicationErrors.TripNotFound;
        }

        if (trip.HasDeparted(nowUtc))
        {
            return ApplicationErrors.TripDeparted;
        }

        if (requireActiveSchedule && !await context.Schedules.AnyAsync(s => s.Id == trip.ScheduleId && s.IsActive, ct))
        {
            return ApplicationErrors.TripNotBookable;
        }

        var layout = trip.Bus!.SeatLayout!;
        var notInLayout = request.SeatNumbers.Where(n => !layout.HasSeat(n)).Order().ToList();

        if (notInLayout.Count > 0)
        {
            return ApplicationErrors.SeatsNotInLayout(notInLayout);
        }

        var routeStops = await context.RouteStops
            .AsNoTracking()
            .Where(r => r.Direction == trip.Direction)
            .ToListAsync(ct);

        var pickup = routeStops.FirstOrDefault(r => r.StopId == request.PickupStopId);
        var dropoff = routeStops.FirstOrDefault(r => r.StopId == request.DropoffStopId);

        if (pickup is null || dropoff is null)
        {
            return ApplicationErrors.StopsNotOnRoute;
        }

        if (pickup.Order >= dropoff.Order)
        {
            return ApplicationErrors.PickupAfterDropoff;
        }

        await ExpireOverdueHoldsAsync(context, trip.Id, nowUtc, ct);

        var taken = await context.BookingSeats
            .Where(s => s.TripId == trip.Id && s.IsActive && request.SeatNumbers.Contains(s.SeatNumber))
            .Select(s => s.SeatNumber)
            .OrderBy(n => n)
            .ToListAsync(ct);

        if (taken.Count > 0)
        {
            return ApplicationErrors.SeatsTaken(taken);
        }

        return trip;
    }

    public static async Task<Result<BookingCreatedDto>> SaveAsync(IAppDbContext context, Booking booking, CancellationToken ct)
    {
        context.Bookings.Add(booking);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException ex) when (ex.ConstraintName == DbConstraintNames.ActiveSeatPerTrip)
        {
            return ApplicationErrors.SeatsJustTaken;
        }

        return new BookingCreatedDto(booking.Id, booking.PublicToken);
    }

    private static async Task ExpireOverdueHoldsAsync(IAppDbContext context, Guid tripId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var overdue = await context.Bookings
            .Include(b => b.Seats)
            .Where(b => b.TripId == tripId && b.Status == BookingStatus.HoldPendingPayment && b.HoldExpiresAtUtc <= nowUtc)
            .ToListAsync(ct);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var booking in overdue)
        {
            booking.Expire(nowUtc);
        }

        await context.SaveChangesAsync(ct);
    }
}
