using System.Net;
using System.Net.Mail;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using SBus.Infrastructure.Identity;

namespace SBus.Web.Services;

public interface IAccountEmail
{
    Task SendConfirmationAsync(AppUser user, CancellationToken ct);
    Task SendResetAsync(AppUser user, CancellationToken ct);
}


