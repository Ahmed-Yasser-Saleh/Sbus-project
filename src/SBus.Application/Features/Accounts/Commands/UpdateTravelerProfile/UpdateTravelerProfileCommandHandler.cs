using MediatR;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Validation;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;

public sealed class UpdateTravelerProfileCommandHandler(ITravelerProfiles profiles)
    : IRequestHandler<UpdateTravelerProfileCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(UpdateTravelerProfileCommand request, CancellationToken ct) =>
        profiles.UpdateCurrentAsync(request.FullName!.Trim(), TravelerProfileRules.NormalizePhone(request.PhoneNumber)!, ct);
}
