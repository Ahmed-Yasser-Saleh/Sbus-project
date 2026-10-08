using MediatR;

using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Routes.Commands.SetRouteStops;

public sealed record SetRouteStopsCommand(Direction Direction, List<RouteStopItem> Stops) : IRequest<Result<Updated>>;
