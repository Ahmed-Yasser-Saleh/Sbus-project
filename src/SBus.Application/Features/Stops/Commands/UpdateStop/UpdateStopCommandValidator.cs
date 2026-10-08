using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Stops.Commands.UpdateStop;

public sealed class UpdateStopCommandValidator : AbstractValidator<UpdateStopCommand>
{
    public UpdateStopCommandValidator()
    {
        RuleFor(x => x.StopId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المحطة مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(300);
    }
}
