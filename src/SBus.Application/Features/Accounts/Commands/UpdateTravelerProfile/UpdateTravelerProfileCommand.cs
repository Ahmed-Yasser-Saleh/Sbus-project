using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;

public sealed record UpdateTravelerProfileCommand(string? FullName, string? PhoneNumber) : IRequest<Result<Success>>
{
    public override string ToString() => nameof(UpdateTravelerProfileCommand);
}
