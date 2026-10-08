using SBus.Domain.Common;
using SBus.Domain.Common.Results;
using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;

namespace SBus.Domain.Trips;

public sealed class Trip : AuditableEntity
{
    public Guid ScheduleId { get; private set; }
    public Direction Direction { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public TimeOnly DepartureTime { get; private set; }
    public DateTimeOffset DepartureAtUtc { get; private set; }
    public decimal Price { get; private set; }
    public Guid BusId { get; private set; }
    public Bus? Bus { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    private Trip()
    { }

    private Trip(Guid id, Guid scheduleId, Direction direction, DateOnly serviceDate, TimeOnly departureTime, DateTimeOffset departureAtUtc, decimal price, Guid busId, Guid driverId)
        : base(id)
    {
        ScheduleId = scheduleId;
        Direction = direction;
        ServiceDate = serviceDate;
        DepartureTime = departureTime;
        DepartureAtUtc = departureAtUtc;
        Price = price;
        BusId = busId;
        DriverId = driverId;
    }

    public static Result<Trip> Create(Guid id, Guid scheduleId, Direction direction, DateOnly serviceDate, TimeOnly departureTime, DateTimeOffset departureAtUtc, decimal price, Guid busId, Guid driverId)
    {
        if (scheduleId == Guid.Empty)
        {
            return TripErrors.ScheduleRequired;
        }

        if (!Enum.IsDefined(direction))
        {
            return TripErrors.DirectionInvalid;
        }

        if (price <= 0)
        {
            return TripErrors.PriceInvalid;
        }

        if (busId == Guid.Empty)
        {
            return TripErrors.BusRequired;
        }

        if (driverId == Guid.Empty)
        {
            return TripErrors.DriverRequired;
        }

        return new Trip(id, scheduleId, direction, serviceDate, departureTime, departureAtUtc, price, busId, driverId);
    }

    public bool HasDeparted(DateTimeOffset nowUtc) => nowUtc >= DepartureAtUtc;

    public Result<Updated> ApplySchedule(TimeOnly departureTime, DateTimeOffset departureAtUtc, decimal price, Guid busId, Guid driverId)
    {
        if (price <= 0)
        {
            return TripErrors.PriceInvalid;
        }

        var assignment = UpdateAssignment(busId, driverId);

        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        DepartureTime = departureTime;
        DepartureAtUtc = departureAtUtc;
        Price = price;

        return Result.Updated;
    }

    public Result<Updated> UpdateAssignment(Guid busId, Guid driverId)
    {
        if (busId == Guid.Empty)
        {
            return TripErrors.BusRequired;
        }

        if (driverId == Guid.Empty)
        {
            return TripErrors.DriverRequired;
        }

        BusId = busId;
        DriverId = driverId;

        return Result.Updated;
    }
}
