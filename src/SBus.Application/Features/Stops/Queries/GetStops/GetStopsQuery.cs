using MediatR;

using SBus.Application.Features.Stops.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Stops.Queries.GetStops;

public sealed record GetStopsQuery(bool ActiveOnly = false, SBus.Application.Common.Models.QueryPage? Page = null) : IRequest<Result<List<StopDto>>>;
