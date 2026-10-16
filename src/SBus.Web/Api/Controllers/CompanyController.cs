using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SBus.Application.Common.Security;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/company/status")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.Company)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class CompanyController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CompanyStatusResponse>), 200)]
    public ApiResponse<CompanyStatusResponse> Get() => new(new(Available: false));
}