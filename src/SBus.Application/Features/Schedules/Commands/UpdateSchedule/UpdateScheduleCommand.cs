using MediatR;

using SBus.Application.Features.Schedules.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Commands.UpdateSchedule;

public sealed record UpdateScheduleCommand(
    Guid ScheduleId,
    TimeOnly DepartureTime,
    decimal Price,
    Guid BusId,
    Guid DriverId,
    bool IsActive) : IRequest<Result<ScheduleUpdatedDto>>;
