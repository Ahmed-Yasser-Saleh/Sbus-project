using System.Net.Mail;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

using SBus.Application.Features.Accounts;
using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public sealed class AccountRecoveryService(UserManager<AppUser> users, IAccountEmail email, ILogger<AccountRecoveryService> logger) : IAccountRecovery
{
    public Task SendConfirmationAsync(string address, CancellationToken ct) => SendAsync(address, false, ct);
    public Task SendResetAsync(string address, CancellationToken ct) => SendAsync(address, true, ct);

    public async Task<bool> ConfirmAsync(string userId, string code, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        try
        {
            return (await users.ConfirmEmailAsync(user, Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)))).Succeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<bool> ResetAsync(string userId, string code, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId);
        if (user is null || !user.EmailConfirmed || !await users.HasPasswordAsync(user))
        {
            return false;
        }

        try
        {
            return (await users.ResetPasswordAsync(user, Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)), password)).Succeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task SendAsync(string address, bool reset, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(address.Trim());
        if (user is null || (reset ? !user.EmailConfirmed || !await users.HasPasswordAsync(user) : user.EmailConfirmed))
        {
            return;
        }

        try
        {
            if (reset)
            {
                await email.SendResetAsync(user, ct);
            }
            else
            {
                await email.SendConfirmationAsync(user, ct);
            }
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            logger.LogError("Account email delivery failed ({FailureType}).", exception.GetType().Name);
        }
    }
}