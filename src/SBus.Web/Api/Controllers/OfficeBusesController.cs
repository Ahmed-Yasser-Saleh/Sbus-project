using Microsoft.AspNetCore.Mvc;
using SBus.Application.Features.Buses.Commands.CreateBus;
using SBus.Application.Features.Buses.Commands.UpdateBus;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/buses")]
public sealed class OfficeBusesController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<BusDto>), 200)]
    public async Task<PagedResponse<BusDto>> List([FromQuery] PageRequest pagination, [FromQuery] bool activeOnly, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetBusesQuery(activeOnly, ApiPages.Query(pagination)), ct), pagination);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ApiResponse<Guid>> Create(CreateBusCommand request, CancellationToken ct) =>
        new(await sender.Send(request, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ApiResponse<Updated>> Update(Guid id, UpdateBusRequest request, CancellationToken ct) =>
        new(await sender.Send(new UpdateBusCommand(id, request.PlateNumber, request.SeatLayoutId, request.IsActive), ct));
}

