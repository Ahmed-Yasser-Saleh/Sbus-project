using MediatR;

using SBus.Application.Features.Schedules.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Queries.GetSchedules;

public sealed record GetSchedulesQuery : IRequest<Result<List<ScheduleDto>>>;
