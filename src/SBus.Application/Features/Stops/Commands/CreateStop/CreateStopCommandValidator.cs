using FluentValidation;

using SBus.Domain.Common.Constants;

namespace SBus.Application.Features.Stops.Commands.CreateStop;

public sealed class CreateStopCommandValidator : AbstractValidator<CreateStopCommand>
{
    public CreateStopCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المحطة مطلوب.")
            .MaximumLength(SBusConstants.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(300);
    }
}
