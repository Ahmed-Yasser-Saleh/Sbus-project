using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBus.Application.Common.Security;
using SBus.Application.Features.Bookings.Queries.GetMyTickets;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/me/tickets")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Traveler)]
[ServiceFilter(typeof(CompleteApiProfileFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 422)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class MyTicketsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MyTicketDto>), 200)]
    public async Task<PagedResponse<MyTicketDto>> Get([Range(1, 10000)] int page = 1, CancellationToken ct = default)
    {
        var tickets = await sender.Send(new GetMyTicketsQuery(page), ct);
        return new(tickets, new(page, 20, tickets.Count == 20));
    }
}
