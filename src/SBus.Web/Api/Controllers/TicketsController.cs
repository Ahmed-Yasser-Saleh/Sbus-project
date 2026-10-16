using System.ComponentModel.DataAnnotations;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using SBus.Application.Common.Exceptions;
using SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;
using SBus.Application.Features.Bookings.Commands.SubmitReceipt;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetTicket;
using SBus.Web.Api.Contracts;
using SBus.Web.Api.Errors;
using SBus.Web.Infrastructure;

namespace SBus.Web.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/tickets/{token}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(RateLimitPolicies.PublicWrites)]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class TicketsController(ISender sender, ApiResultAdapter results) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), 200)]
    public async Task<ApiResponse<TicketDto>> Get([StringLength(128)] string token, CancellationToken ct) =>
        new(results.Value(await sender.Send(new GetTicketQuery(token), ct)));

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<ApiResponse<bool>> Cancel([StringLength(128)] string token, CancellationToken ct)
    {
        results.Value(await sender.Send(new CancelBookingByPassengerCommand(token), ct));
        return new(true);
    }

    [HttpPost("receipt")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 413)]
    [ProducesResponseType(typeof(ProblemDetails), 415)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<ApiResponse<bool>> Receipt([StringLength(128)] string token, IFormFile? receipt, CancellationToken ct)
    {
        if (receipt is null || receipt.Length == 0)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["receipt"] = ["Select your payment receipt."] });
        }

        await using var stream = receipt.OpenReadStream();
        results.Value(await sender.Send(new SubmitReceiptCommand(token, stream, receipt.Length), ct));
        return new(true);
    }
}