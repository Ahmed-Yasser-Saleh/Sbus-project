using Microsoft.AspNetCore.Mvc;

using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Commands.UpdateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Queries.GetStops;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/stops")]
public sealed class OfficeStopsController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StopDto>), 200)]
    public async Task<PagedResponse<StopDto>> List([FromQuery] PageRequest pagination, [FromQuery] bool activeOnly, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetStopsQuery(activeOnly, ApiPages.Query(pagination)), ct), pagination);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StopDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ApiResponse<StopDto>> Create(CreateStopCommand request, CancellationToken ct) =>
        new(await sender.Send(request, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ApiResponse<Updated>> Update(Guid id, UpdateStopRequest request, CancellationToken ct) =>
        new(await sender.Send(new UpdateStopCommand(id, request.Name, request.Description, request.IsActive), ct));
}