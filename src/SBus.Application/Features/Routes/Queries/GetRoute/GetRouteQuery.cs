using MediatR;

using SBus.Application.Features.Routes.Dtos;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Routes.Queries.GetRoute;

public sealed record GetRouteQuery(Direction Direction) : IRequest<Result<List<RouteStopDto>>>;
