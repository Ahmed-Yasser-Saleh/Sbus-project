using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Routes.Commands.SetRouteStops;

public class SetRouteStopsCommandHandler(
    ILogger<SetRouteStopsCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<SetRouteStopsCommand, Result<Updated>>
{
    private readonly ILogger<SetRouteStopsCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<Updated>> Handle(SetRouteStopsCommand command, CancellationToken ct)
    {
        var stopIds = command.Stops.Select(s => s.StopId).Distinct().ToList();

        var activeCount = await _context.Stops.CountAsync(s => stopIds.Contains(s.Id) && s.IsActive, ct);

        if (activeCount != stopIds.Count)
        {
            return ApplicationErrors.StopsNotFoundOrInactive;
        }

        var sequence = RouteStop.CreateSequence(
            command.Direction,
            [.. command.Stops.Select(s => (s.StopId, s.MinutesFromStart))]);

        if (sequence.IsError)
        {
            return sequence.Errors;
        }

        var existing = await _context.RouteStops
            .Where(r => r.Direction == command.Direction)
            .ToListAsync(ct);

        _context.RouteStops.RemoveRange(existing);
        _context.RouteStops.AddRange(sequence.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Route {Direction} set with {Count} stops", command.Direction, sequence.Value.Count);

        return Result.Updated;
    }
}
