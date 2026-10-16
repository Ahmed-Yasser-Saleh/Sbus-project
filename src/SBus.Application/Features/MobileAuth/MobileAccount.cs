namespace SBus.Application.Features.MobileAuth;

public sealed record MobileAccount(string Id, IReadOnlyList<string> Roles, string SecurityStampHash, bool ProfileComplete);