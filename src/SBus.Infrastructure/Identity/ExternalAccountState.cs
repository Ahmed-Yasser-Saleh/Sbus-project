using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Exceptions;

namespace SBus.Infrastructure.Identity;

public enum ExternalAccountState
{
    Rejected,
    Ready,
    PasswordProofRequired,
}
