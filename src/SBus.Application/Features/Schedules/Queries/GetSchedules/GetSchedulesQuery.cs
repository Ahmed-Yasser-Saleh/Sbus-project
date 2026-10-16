using MediatR;

using SBus.Application.Features.Schedules.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Queries.GetSchedules;

public sealed record GetSchedulesQuery(SBus.Application.Common.Models.QueryPage? Page = null) : IRequest<Result<List<ScheduleDto>>>;
