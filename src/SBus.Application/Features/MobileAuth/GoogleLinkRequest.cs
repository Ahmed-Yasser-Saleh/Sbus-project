namespace SBus.Application.Features.MobileAuth;

public sealed record GoogleLinkRequest(string LinkingProof, string Password);