namespace SBus.Application.Features.MobileAuth;

public sealed record MobileTokens(string AccessToken, string RefreshToken, DateTimeOffset AccessExpiresAtUtc, DateTimeOffset RefreshExpiresAtUtc, MobileAccount Account);