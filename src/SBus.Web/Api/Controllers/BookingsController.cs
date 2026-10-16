using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SBus.Application.Common.Security;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Web.Api.Contracts;
using SBus.Web.Api.Errors;
using SBus.Web.Infrastructure;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Traveler)]
[ServiceFilter(typeof(CompleteApiProfileFilter))]
[EnableRateLimiting(RateLimitPolicies.PublicWrites)]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 422)]
[ProducesResponseType(typeof(ProblemDetails), 429)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class BookingsController(ISender sender, ApiResultAdapter results) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BookingCreatedDto>), 200)]
    public async Task<ApiResponse<BookingCreatedDto>> Create(CreateOnlineBookingCommand request, CancellationToken ct) =>
        new(results.Value(await sender.Send(request, ct)));
}
