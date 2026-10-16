using SBus.Application.Features.MobileAuth;
using SBus.Web.Services;

namespace SBus.Web.Api.Contracts;

public sealed record SessionResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAtUtc,
    DateTimeOffset RefreshExpiresAtUtc, string UserId, IReadOnlyList<string> Roles, string Destination, bool RequiresProfileCompletion);