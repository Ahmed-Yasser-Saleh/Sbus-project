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

public sealed record MobileLoginOptions(bool GoogleEnabled, bool RequireConfirmedEmail, int PasswordMinLength, int PasswordMaxLength);