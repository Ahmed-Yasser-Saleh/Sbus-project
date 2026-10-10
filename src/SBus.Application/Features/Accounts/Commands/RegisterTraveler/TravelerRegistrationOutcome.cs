namespace SBus.Application.Features.Accounts.Commands.RegisterTraveler;

// The created user's identifier stays internal to the registration workflow.
public sealed record TravelerRegistrationOutcome(string? UserId);

