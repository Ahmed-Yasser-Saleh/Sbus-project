using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Web.Api.Contracts;
using SBus.Web.Api.Errors;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/me/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Traveler)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class ProfileController(ISender sender, ITravelerProfiles profiles, ApiResultAdapter results) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TravelerProfileDto>), 200)]
    public async Task<ApiResponse<TravelerProfileDto>> Get(CancellationToken ct) =>
        new(await profiles.GetCurrentAsync(ct) ?? throw new ForbiddenException());

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<TravelerProfileDto>), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ApiResponse<TravelerProfileDto>> Update(UpdateTravelerProfileCommand request, CancellationToken ct)
    {
        results.Value(await sender.Send(request, ct));
        return await Get(ct);
    }
}