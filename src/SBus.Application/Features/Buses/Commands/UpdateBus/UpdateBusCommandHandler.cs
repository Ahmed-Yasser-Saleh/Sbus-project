using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Buses.Common;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Commands.UpdateBus;

public class UpdateBusCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<UpdateBusCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(UpdateBusCommand command, CancellationToken ct)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == command.BusId, ct);

        if (bus is null)
        {
            return ApplicationErrors.BusNotFound;
        }

        if (!await _context.SeatLayouts.AnyAsync(l => l.Id == command.SeatLayoutId, ct))
        {
            return ApplicationErrors.SeatLayoutNotFound;
        }

        var plate = command.PlateNumber.Trim();

        if (await _context.Buses.AnyAsync(b => b.PlateNumber == plate && b.Id != command.BusId, ct))
        {
            return ApplicationErrors.PlateNumberExists;
        }

        if (bus.SeatLayoutId != command.SeatLayoutId)
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var upcomingTrips = _context.Trips
                .Where(t => t.BusId == bus.Id && t.DepartureAtUtc > nowUtc)
                .Select(t => t.Id);

            var coverage = await BookedSeatsCoverage.EnsureLayoutCoversAsync(_context, command.SeatLayoutId, upcomingTrips, ct);

            if (coverage.IsError)
            {
                return coverage.Errors;
            }
        }

        var result = bus.Update(plate, command.SeatLayoutId, command.IsActive);

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}
