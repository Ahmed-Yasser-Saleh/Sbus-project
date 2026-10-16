using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SBus.Application.Common.Security;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Application.Features.Trips.Queries.SearchTrips;
using SBus.Web.Api.Contracts;
using SBus.Web.Api.Errors;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class TripsController(ISender sender, ApiResultAdapter results) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TripSearchResponse), 200)]
    public async Task<TripSearchResponse> Search([FromQuery] SearchTripsQuery query, CancellationToken ct)
    {
        var response = results.Value(await sender.Send(query, ct));
        return new(response.Trips, new(response.Page, response.PageSize, response.HasMore), response.Direction, response.Date, response.Days);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Traveler)]
    [ServiceFilter(typeof(CompleteApiProfileFilter))]
    [ProducesResponseType(typeof(ApiResponse<TripBookingDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<ApiResponse<TripBookingDto>> Details(Guid id, CancellationToken ct) =>
        new(results.Value(await sender.Send(new GetTripForBookingQuery(id), ct)));
}