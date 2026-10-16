using FluentValidation;

namespace SBus.Application.Features.Trips.Queries.SearchTrips;

public sealed class SearchTripsQueryValidator : AbstractValidator<SearchTripsQuery>
{
    public SearchTripsQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 10000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
