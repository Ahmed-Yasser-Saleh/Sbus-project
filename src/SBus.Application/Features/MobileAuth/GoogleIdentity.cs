namespace SBus.Application.Features.MobileAuth;

public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? HostedDomain, string? Name);