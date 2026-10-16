using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;
using SBus.Web.Api.Authentication;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 413)]
[ProducesResponseType(typeof(ProblemDetails), 415)]
[ProducesResponseType(typeof(ProblemDetails), 429)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
[EnableRateLimiting(ApiRegistration.AuthRatePolicy)]
public sealed class MobileAuthController(IMobileAuthService auth, IOptions<MobileAuthOptions> options) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SessionResponse>), 200)]
    public async Task<ApiResponse<SessionResponse>> Login(LoginRequest request, CancellationToken ct) => new(SessionMapper.Map(await auth.LoginAsync(request, ct)));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SessionResponse>), 200)]
    public async Task<ApiResponse<SessionResponse>> Refresh(RefreshRequest request, CancellationToken ct) => new(SessionMapper.Map(await auth.RefreshAsync(request, ct)));

    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GoogleResponse>), 200)]
    public async Task<ApiResponse<GoogleResponse>> Google(GoogleRequest request, CancellationToken ct) => new(SessionMapper.Map(await auth.GoogleAsync(request, ct)));

    [HttpPost("google/link")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SessionResponse>), 200)]
    public async Task<ApiResponse<SessionResponse>> LinkGoogle(GoogleLinkRequest request, CancellationToken ct) => new(SessionMapper.Map(await auth.LinkGoogleAsync(request, ct)));

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ApiResponse<bool>> Logout(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthenticatedException();
        if (!Guid.TryParse(User.FindFirstValue(MobileClaims.FamilyId), out var familyId))
        {
            throw new UnauthenticatedException();
        }

        await auth.LogoutAsync(userId, familyId, ct);
        return new(true);
    }

    [HttpGet("options")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<MobileLoginOptions>), 200)]
    public ApiResponse<MobileLoginOptions> Options([FromServices] IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> identity) =>
        new(new(options.Value.GoogleClientIds.Length > 0, identity.Value.SignIn.RequireConfirmedEmail,
            SBus.Application.Common.Validation.AccountPasswordPolicy.MinLength, SBus.Application.Common.Validation.AccountPasswordPolicy.MaxLength));
}