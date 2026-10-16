namespace SBus.Application.Features.MobileAuth;

public sealed record MobileGoogleResult(MobileGoogleState State, MobileTokens? Tokens, string? LinkingProof);