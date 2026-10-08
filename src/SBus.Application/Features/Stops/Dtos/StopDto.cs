namespace SBus.Application.Features.Stops.Dtos;

public sealed record StopDto(Guid StopId, string Name, string? Description, bool IsActive);
