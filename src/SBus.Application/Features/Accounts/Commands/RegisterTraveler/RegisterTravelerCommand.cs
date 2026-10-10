using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Accounts.Commands.RegisterTraveler;

public sealed record RegisterTravelerCommand(string? Email, string? Password, string? ConfirmPassword)
    : IRequest<Result<Success>>
{
    // Commands contain credentials; prevent accidental disclosure in structured logs.
    public override string ToString() => nameof(RegisterTravelerCommand);
}

