using Microsoft.AspNetCore.Mvc;

using SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/seat-layouts")]
public sealed class OfficeSeatLayoutsController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SeatLayoutDto>), 200)]
    public async Task<PagedResponse<SeatLayoutDto>> List([FromQuery] PageRequest pagination, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetSeatLayoutsQuery(ApiPages.Query(pagination)), ct), pagination);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SeatLayoutDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ApiResponse<SeatLayoutDto>> Create(CreateSeatLayoutCommand request, CancellationToken ct) =>
        new(await sender.Send(request, ct));
}