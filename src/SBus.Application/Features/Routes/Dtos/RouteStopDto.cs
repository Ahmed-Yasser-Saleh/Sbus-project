namespace SBus.Application.Features.Routes.Dtos;

public sealed record RouteStopDto(Guid StopId, string StopName, int Order, int MinutesFromStart);
