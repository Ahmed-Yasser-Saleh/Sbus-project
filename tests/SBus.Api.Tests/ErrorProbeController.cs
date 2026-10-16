using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Security;

namespace SBus.Api.Tests;

// Registered only in the test host; production does not discover this assembly.
[ApiController]
[Route("api/v1/probe")]
public sealed class ErrorProbeController : ControllerBase
{
    [HttpGet("{category}")]
    public IActionResult Error(string category) => throw category switch
    {
        "validation" => new ValidationException([new ValidationFailure("Stops[0].MinutesFromStart", "Invalid minutes.")]),
        "not-found" => new NotFoundException("Missing trip.", "TRIP_NOT_FOUND"),
        "conflict" => new ConflictException("Seat unavailable.", "BOOKING_SEAT_UNAVAILABLE"),
        "business-rule" => new BusinessRuleException("Complete your profile.", "TRAVELER_PROFILE_INCOMPLETE"),
        "unauthenticated" => new UnauthenticatedException(),
        "forbidden" => new ForbiddenException(),
        "unavailable" => new ServiceUnavailableException("Database credentials must remain private."),
        "concurrency" => new DbUpdateConcurrencyException("SQL internal table name"),
        "unique" => new UniqueConstraintException("private-index", new Exception("SQL")),
        "body-size" => new BadHttpRequestException("Body too large", 413),
        _ => new InvalidOperationException("SQL password=private; stack details"),
    };

    [HttpGet("traveler")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Traveler)]
    public IActionResult Traveler() => Ok(new { userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value });

    [HttpGet("owner")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.CompanyOwner)]
    public IActionResult Owner() => Ok();

    [HttpGet("employee")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.CompanyEmployee)]
    public IActionResult Employee() => Ok();

    [HttpGet("/web-error-probe")]
    public IActionResult WebError() => throw new InvalidOperationException("Web private diagnostics.");
}