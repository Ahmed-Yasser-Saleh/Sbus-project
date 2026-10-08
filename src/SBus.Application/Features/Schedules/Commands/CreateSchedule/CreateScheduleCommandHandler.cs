using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Schedules.Common;
using SBus.Domain.Common.Results;
using SBus.Domain.Schedules;

namespace SBus.Application.Features.Schedules.Commands.CreateSchedule;

public class CreateScheduleCommandHandler(
    ILogger<CreateScheduleCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CreateScheduleCommand, Result<Guid>>
{
    private readonly ILogger<CreateScheduleCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<Guid>> Handle(CreateScheduleCommand command, CancellationToken ct)
    {
        var assignment = await ScheduleAssignmentRules.EnsureBusAndDriverActiveAsync(_context, command.BusId, command.DriverId, ct);

        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        if (await _context.Schedules.AnyAsync(s => s.Direction == command.Direction && s.DepartureTime == command.DepartureTime, ct))
        {
            return ApplicationErrors.ScheduleExists;
        }

        var result = Schedule.Create(Guid.CreateVersion7(), command.Direction, command.DepartureTime, command.Price, command.BusId, command.DriverId);

        if (result.IsError)
        {
            return result.Errors;
        }

        _context.Schedules.Add(result.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Schedule created. Id: {ScheduleId}", result.Value.Id);

        return result.Value.Id;
    }
}
