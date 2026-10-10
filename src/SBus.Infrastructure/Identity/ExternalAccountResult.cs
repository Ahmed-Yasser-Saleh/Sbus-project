using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Exceptions;

namespace SBus.Infrastructure.Identity;

public sealed record ExternalAccountResult(ExternalAccountState State, AppUser? User = null);
