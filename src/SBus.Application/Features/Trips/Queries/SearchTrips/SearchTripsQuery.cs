using MediatR;
using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Queries.SearchTrips;

public sealed record SearchTripsQuery(Direction Direction = Direction.CairoToSuez, DateOnly? Date = null, int Page = 1, int PageSize = 20)
    : IRequest<Result<TripSearchDto>>;
