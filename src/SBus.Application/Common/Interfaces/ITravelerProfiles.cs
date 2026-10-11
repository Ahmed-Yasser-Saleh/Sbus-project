using SBus.Application.Features.Accounts.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Common.Interfaces;

public interface ITravelerProfiles
{
    Task<TravelerProfileDto?> GetCurrentAsync(CancellationToken ct);
    Task<Result<Success>> UpdateCurrentAsync(string fullName, string phoneNumber, CancellationToken ct);
}
