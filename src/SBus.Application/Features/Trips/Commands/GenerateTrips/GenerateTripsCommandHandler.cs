using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Time;
using SBus.Domain.Common.Results;
using SBus.Domain.Trips;

namespace SBus.Application.Features.Trips.Commands.GenerateTrips;

public class GenerateTripsCommandHandler(
    ILogger<GenerateTripsCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<GenerateTripsCommand, Result<int>>
{
    private readonly ILogger<GenerateTripsCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<int>> Handle(GenerateTripsCommand command, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var firstDate = CairoTime.Today(nowUtc);
        var lastDate = firstDate.AddDays(command.DaysAhead - 1);

        var schedules = await _context.Schedules
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var existing = (await _context.Trips
                .Where(t => t.ServiceDate >= firstDate && t.ServiceDate <= lastDate)
                .Select(t => new { t.ScheduleId, t.ServiceDate })
                .ToListAsync(ct))
            .Select(t => (t.ScheduleId, t.ServiceDate))
            .ToHashSet();

        var created = 0;

        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            foreach (var schedule in schedules)
            {
                if (existing.Contains((schedule.Id, date)))
                {
                    continue;
                }

                var departureAtUtc = CairoTime.ToUtc(date, schedule.DepartureTime);

                if (departureAtUtc <= nowUtc)
                {
                    continue;
                }

                var trip = Trip.Create(
                    Guid.CreateVersion7(),
                    schedule.Id,
                    schedule.Direction,
                    date,
                    schedule.DepartureTime,
                    departureAtUtc,
                    schedule.Price,
                    schedule.BusId,
                    schedule.DriverId);

                if (trip.IsError)
                {
                    _logger.LogWarning("Skipped trip for schedule {ScheduleId} on {Date}: {Error}", schedule.Id, date, trip.TopError.Code);
                    continue;
                }

                _context.Trips.Add(trip.Value);
                created++;
            }
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Generated {Count} trips from {FirstDate} to {LastDate}", created, firstDate, lastDate);
        }

        return created;
    }
}
