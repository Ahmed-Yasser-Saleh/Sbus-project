using SBus.Domain.Routes;

namespace SBus.Application.Features.Schedules.Dtos;

public sealed record ScheduleDto(
    Guid ScheduleId,
    Direction Direction,
    TimeOnly DepartureTime,
    decimal Price,
    Guid BusId,
    string BusPlateNumber,
    Guid DriverId,
    string DriverName,
    bool IsActive);
