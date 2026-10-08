using MediatR;

using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Queries.GetAvailableTrips;

public sealed record GetAvailableTripsQuery(Direction Direction, DateOnly Date) : IRequest<Result<List<TripSummaryDto>>>;
