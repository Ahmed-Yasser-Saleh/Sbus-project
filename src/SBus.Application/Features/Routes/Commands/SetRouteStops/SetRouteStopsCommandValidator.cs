using FluentValidation;

namespace SBus.Application.Features.Routes.Commands.SetRouteStops;

public sealed class SetRouteStopsCommandValidator : AbstractValidator<SetRouteStopsCommand>
{
    public SetRouteStopsCommandValidator()
    {
        RuleFor(x => x.Direction).IsInEnum();

        RuleFor(x => x.Stops)
            .NotNull()
            .Must(s => s.Count >= 2).WithMessage("الخط لازم يكون فيه محطتين على الأقل.");

        RuleForEach(x => x.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.StopId).NotEmpty().WithMessage("اختار المحطة.");
            stop.RuleFor(s => s.MinutesFromStart).InclusiveBetween(0, 24 * 60).WithMessage("الوقت لازم يكون بين 0 و 1440 دقيقة.");
        });
    }
}
