using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Time;
using SBus.Application.Features.Schedules.Common;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Commands.UpdateSchedule;

public class UpdateScheduleCommandHandler(
    ILogger<UpdateScheduleCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateScheduleCommand, Result<ScheduleUpdatedDto>>
{
    private readonly ILogger<UpdateScheduleCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<ScheduleUpdatedDto>> Handle(UpdateScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == command.ScheduleId, ct);

        if (schedule is null)
        {
            return ApplicationErrors.ScheduleNotFound;
        }

        var assignment = await ScheduleAssignmentRules.EnsureBusAndDriverActiveAsync(_context, command.BusId, command.DriverId, ct);

        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var duplicate = await _context.Schedules.AnyAsync(
            s => s.Id != schedule.Id && s.Direction == schedule.Direction && s.DepartureTime == command.DepartureTime,
            ct);

        if (duplicate)
        {
            return ApplicationErrors.ScheduleExists;
        }

        var result = schedule.Update(command.DepartureTime, command.Price, command.BusId, command.DriverId, command.IsActive);

        if (result.IsError)
        {
            return result.Errors;
        }

        var nowUtc = _timeProvider.GetUtcNow();

        var futureTrips = await _context.Trips
            .Where(t => t.ScheduleId == schedule.Id && t.DepartureAtUtc > nowUtc)
            .ToListAsync(ct);

        var futureTripIds = futureTrips.Select(t => t.Id).ToList();

        var tripsWithBookings = (await _context.BookingSeats
                .Where(s => s.IsActive && futureTripIds.Contains(s.TripId))
                .Select(s => s.TripId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var updated = 0;

        foreach (var trip in futureTrips.Where(t => schedule.IsActive && !tripsWithBookings.Contains(t.Id)))
        {
            var departureAtUtc = CairoTime.ToUtc(trip.ServiceDate, schedule.DepartureTime);

            if (departureAtUtc <= nowUtc)
            {
                continue;
            }

            trip.ApplySchedule(schedule.DepartureTime, departureAtUtc, schedule.Price, schedule.BusId, schedule.DriverId);
            updated++;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Schedule {ScheduleId} updated. Trips updated: {Updated}, kept because of bookings: {Kept}",
            schedule.Id,
            updated,
            tripsWithBookings.Count);

        return new ScheduleUpdatedDto(updated, tripsWithBookings.Count);
    }
}
