using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Buses.Common;
using SBus.Application.Features.Schedules.Common;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Commands.UpdateTripAssignment;

public class UpdateTripAssignmentCommandHandler(
    ILogger<UpdateTripAssignmentCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateTripAssignmentCommand, Result<Updated>>
{
    private readonly ILogger<UpdateTripAssignmentCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(UpdateTripAssignmentCommand command, CancellationToken ct)
    {
        var trip = await _context.Trips
            .Include(t => t.Bus)
            .FirstOrDefaultAsync(t => t.Id == command.TripId, ct);

        if (trip is null)
        {
            return ApplicationErrors.TripNotFound;
        }

        if (trip.HasDeparted(_timeProvider.GetUtcNow()))
        {
            return ApplicationErrors.TripDeparted;
        }

        var assignment = await ScheduleAssignmentRules.EnsureBusAndDriverActiveAsync(_context, command.BusId, command.DriverId, ct);

        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        if (command.BusId != trip.BusId)
        {
            var newLayoutId = await _context.Buses
                .Where(b => b.Id == command.BusId)
                .Select(b => b.SeatLayoutId)
                .FirstAsync(ct);

            var coverage = await BookedSeatsCoverage.EnsureLayoutCoversAsync(
                _context,
                newLayoutId,
                _context.Trips.Where(t => t.Id == trip.Id).Select(t => t.Id),
                ct);

            if (coverage.IsError)
            {
                return coverage.Errors;
            }
        }

        var result = trip.UpdateAssignment(command.BusId, command.DriverId);

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Trip {TripId} reassigned to bus {BusId} and driver {DriverId}", trip.Id, command.BusId, command.DriverId);

        return Result.Updated;
    }
}
