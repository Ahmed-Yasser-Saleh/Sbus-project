namespace SBus.Application.Features.Accounts.Dtos;

public sealed record TravelerProfileDto(string? FullName, string? PhoneNumber)
{
    public bool IsComplete => !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(PhoneNumber);
}
