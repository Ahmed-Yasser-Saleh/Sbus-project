using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.Accounts;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Web.Api.Contracts;
using SBus.Web.Api.Errors;

namespace SBus.Web.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
[EnableRateLimiting(ApiRegistration.AuthRatePolicy)]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 429)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class AccountRecoveryController(ISender sender, IAccountRecovery recovery, ApiResultAdapter results) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationResponse>), 200)]
    public async Task<ApiResponse<RegistrationResponse>> Register(
        RegisterTravelerCommand request,
        [FromServices] IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> options, CancellationToken ct)
    {
        results.Value(await sender.Send(request, ct));
        return new(new(options.Value.SignIn.RequireConfirmedEmail));
    }

    [HttpPost("email/resend")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ApiResponse<bool>> Resend(EmailRequest request, CancellationToken ct)
    {
        await recovery.SendConfirmationAsync(request.Email, ct);
        return new(true);
    }

    [HttpPost("password/forgot")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ApiResponse<bool>> Forgot(EmailRequest request, CancellationToken ct)
    {
        await recovery.SendResetAsync(request.Email, ct);
        return new(true);
    }

    [HttpPost("email/confirm")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ApiResponse<bool>> Confirm(ConfirmEmailRequest request, CancellationToken ct)
    {
        if (!await recovery.ConfirmAsync(request.UserId, request.Code, ct))
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["code"] = ["Unable to confirm this request."] }, ErrorCodes.ConfirmationInvalid);
        }

        return new(true);
    }

    [HttpPost("password/reset")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ApiResponse<bool>> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        if (!await recovery.ResetAsync(request.UserId, request.Code, request.Password, ct))
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["code"] = ["Unable to reset password. Request a new link and use a password meeting the requirements."] }, ErrorCodes.PasswordResetInvalid);
        }

        return new(true);
    }
}
