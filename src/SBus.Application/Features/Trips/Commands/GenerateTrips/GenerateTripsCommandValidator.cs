using FluentValidation;

namespace SBus.Application.Features.Trips.Commands.GenerateTrips;

public sealed class GenerateTripsCommandValidator : AbstractValidator<GenerateTripsCommand>
{
    public GenerateTripsCommandValidator()
    {
        RuleFor(x => x.DaysAhead).InclusiveBetween(1, 60);
    }
}
