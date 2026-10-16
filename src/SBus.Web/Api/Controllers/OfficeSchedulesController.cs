using Microsoft.AspNetCore.Mvc;

using SBus.Application.Features.Schedules.Commands.CreateSchedule;
using SBus.Application.Features.Schedules.Commands.UpdateSchedule;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Application.Features.Schedules.Queries.GetSchedules;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/schedules")]
public sealed class OfficeSchedulesController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ScheduleDto>), 200)]
    public async Task<PagedResponse<ScheduleDto>> List([FromQuery] PageRequest pagination, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetSchedulesQuery(ApiPages.Query(pagination)), ct), pagination);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ApiResponse<Guid>> Create(CreateScheduleCommand request, CancellationToken ct) =>
        new(await sender.Send(request, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleUpdatedDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ApiResponse<ScheduleUpdatedDto>> Update(Guid id, UpdateScheduleRequest request, CancellationToken ct) =>
        new(await sender.Send(new UpdateScheduleCommand(id, request.DepartureTime, request.Price, request.BusId, request.DriverId, request.IsActive), ct));
}