using FluentValidation;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;

public sealed class UpdateTravelerProfileCommandValidator : AbstractValidator<UpdateTravelerProfileCommand>
{
    public UpdateTravelerProfileCommandValidator()
    {
        TravelerProfileRules.FullName(RuleFor(x => x.FullName));
        TravelerProfileRules.PhoneNumber(RuleFor(x => x.PhoneNumber));
    }
}
