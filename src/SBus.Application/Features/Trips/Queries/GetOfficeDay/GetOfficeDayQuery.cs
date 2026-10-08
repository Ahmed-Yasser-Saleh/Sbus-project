using MediatR;

using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetOfficeDay;

public sealed record GetOfficeDayQuery(DateOnly Date) : IRequest<Result<List<OfficeTripDto>>>;
