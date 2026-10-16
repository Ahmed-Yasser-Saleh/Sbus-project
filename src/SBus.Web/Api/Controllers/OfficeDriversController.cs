using Microsoft.AspNetCore.Mvc;
using SBus.Application.Features.Drivers.Commands.CreateDriver;
using SBus.Application.Features.Drivers.Commands.UpdateDriver;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/drivers")]
public sealed class OfficeDriversController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<DriverDto>), 200)]
    public async Task<PagedResponse<DriverDto>> List([FromQuery] PageRequest pagination, [FromQuery] bool activeOnly, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetDriversQuery(activeOnly, ApiPages.Query(pagination)), ct), pagination);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<ApiResponse<Guid>> Create(CreateDriverCommand request, CancellationToken ct) =>
        new(await sender.Send(request, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ApiResponse<Updated>> Update(Guid id, UpdateDriverRequest request, CancellationToken ct) =>
        new(await sender.Send(new UpdateDriverCommand(id, request.Name, request.PhoneNumber, request.IsActive), ct));
}

