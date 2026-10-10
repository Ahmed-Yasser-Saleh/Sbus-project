using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Domain.Common.Results;

namespace SBus.Application.Common.Interfaces;

public interface ITravelerRegistration
{
    Task<Result<TravelerRegistrationOutcome>> RegisterAsync(string email, string password, CancellationToken ct);
}

