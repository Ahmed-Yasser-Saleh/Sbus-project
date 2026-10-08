using MediatR;

using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Schedules.Commands.CreateSchedule;

public sealed record CreateScheduleCommand(
    Direction Direction,
    TimeOnly DepartureTime,
    decimal Price,
    Guid BusId,
    Guid DriverId) : IRequest<Result<Guid>>;
