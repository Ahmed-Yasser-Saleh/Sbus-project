using MediatR;

using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;

public sealed record GetSeatLayoutsQuery(SBus.Application.Common.Models.QueryPage? Page = null) : IRequest<Result<List<SeatLayoutDto>>>;
