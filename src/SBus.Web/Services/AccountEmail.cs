using System.Net;
using System.Net.Mail;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public sealed class AccountEmail(UserManager<AppUser> users, IOptions<AccountEmailOptions> options) : IAccountEmail
{
    public async Task SendConfirmationAsync(AppUser user, CancellationToken ct)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await SendAsync(user.Email!, "Confirm your S Bus email",
            Link("/Account/ConfirmEmail", user.Id, token), ct);
    }

    public async Task SendResetAsync(AppUser user, CancellationToken ct)
    {
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await SendAsync(user.Email!, "Reset your S Bus password",
            Link("/Account/ResetPassword", user.Id, token), ct);
    }

    private string Link(string path, string userId, string token) =>
        QueryHelpers.AddQueryString(
            options.Value.PublicOrigin.TrimEnd('/') + path,
            new Dictionary<string, string?>
            {
                ["userId"] = userId,
                ["code"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)),
            });

    private async Task SendAsync(string recipient, string subject, string link, CancellationToken ct)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
            Timeout = 15000,
        };
        using var message = new MailMessage(settings.From, recipient, subject,
            $"Use this link to continue:\n{link}\n\nIf you did not request this, ignore this email.");
        await client.SendMailAsync(message, ct);
    }
}
