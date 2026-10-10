using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Interfaces;
using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public sealed class RegistrationConfirmationSender(
    UserManager<AppUser> users,
    IAccountEmail email,
    ILogger<RegistrationConfirmationSender> logger) : IRegistrationConfirmationSender
{
    public async Task SendAsync(string userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        try
        {
            await email.SendConfirmationAsync(user, ct);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            // Delivery failure must not reveal registration outcome; users can resend.
            logger.LogError("Account email delivery failed ({FailureType}).", ex.GetType().Name);
        }
    }
}

