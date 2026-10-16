using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SBus.Application.Features.Routes.Commands.SetRouteStops;
using SBus.Application.Features.Routes.Dtos;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[Route("api/v1/office/routes/{direction}")]
public sealed class OfficeRoutesController(ApiSender sender) : OfficeApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RouteStopDto>>), 200)]
    public async Task<ApiResponse<List<RouteStopDto>>> Get([EnumDataType(typeof(Direction))] Direction direction, CancellationToken ct) =>
        new(await sender.Send(new GetRouteQuery(direction), ct));

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    public async Task<ApiResponse<Updated>> Update([EnumDataType(typeof(Direction))] Direction direction, RouteStopsRequest request, CancellationToken ct) =>
        new(await sender.Send(new SetRouteStopsCommand(direction, request.Stops), ct));
}

