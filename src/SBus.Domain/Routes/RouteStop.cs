using SBus.Domain.Common;
using SBus.Domain.Common.Results;
using SBus.Domain.Stops;

namespace SBus.Domain.Routes;

public sealed class RouteStop : Entity
{
    public Direction Direction { get; private set; }
    public Guid StopId { get; private set; }
    public Stop? Stop { get; private set; }
    public int Order { get; private set; }
    public int MinutesFromStart { get; private set; }

    private RouteStop()
    { }

    private RouteStop(Guid id, Direction direction, Guid stopId, int order, int minutesFromStart)
        : base(id)
    {
        Direction = direction;
        StopId = stopId;
        Order = order;
        MinutesFromStart = minutesFromStart;
    }

    public static Result<List<RouteStop>> CreateSequence(Direction direction, IReadOnlyList<(Guid StopId, int MinutesFromStart)> stops)
    {
        if (!Enum.IsDefined(direction))
        {
            return RouteStopErrors.DirectionInvalid;
        }

        if (stops is null || stops.Count < 2)
        {
            return RouteStopErrors.AtLeastTwoStops;
        }

        if (stops.Any(s => s.StopId == Guid.Empty))
        {
            return RouteStopErrors.StopRequired;
        }

        if (stops.Select(s => s.StopId).Distinct().Count() != stops.Count)
        {
            return RouteStopErrors.DuplicateStop;
        }

        if (stops[0].MinutesFromStart != 0)
        {
            return RouteStopErrors.FirstStopMustStartAtZero;
        }

        for (var i = 1; i < stops.Count; i++)
        {
            if (stops[i].MinutesFromStart <= stops[i - 1].MinutesFromStart)
            {
                return RouteStopErrors.MinutesMustIncrease;
            }
        }

        return stops
            .Select((s, index) => new RouteStop(Guid.CreateVersion7(), direction, s.StopId, index + 1, s.MinutesFromStart))
            .ToList();
    }
}
