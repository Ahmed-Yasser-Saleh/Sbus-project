using MediatR;

using SBus.Application.Features.Buses.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Queries.GetBuses;

public sealed record GetBusesQuery(bool ActiveOnly = false) : IRequest<Result<List<BusDto>>>;
