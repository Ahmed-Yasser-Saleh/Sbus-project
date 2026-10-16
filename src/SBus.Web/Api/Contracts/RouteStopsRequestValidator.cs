using FluentValidation;

namespace SBus.Web.Api.Contracts;

public sealed class RouteStopsRequestValidator : AbstractValidator<RouteStopsRequest>
{
    public RouteStopsRequestValidator()
    {
        RuleForEach(x => x.Stops).NotNull();
    }
}