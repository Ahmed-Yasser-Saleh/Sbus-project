using SBus.Domain.Common;
using SBus.Domain.Common.Results;
using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;

namespace SBus.Domain.Schedules;

public sealed class Schedule : AuditableEntity
{
    public Direction Direction { get; private set; }
    public TimeOnly DepartureTime { get; private set; }
    public decimal Price { get; private set; }
    public Guid BusId { get; private set; }
    public Bus? Bus { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public bool IsActive { get; private set; }

    private Schedule()
    { }

    private Schedule(Guid id, Direction direction, TimeOnly departureTime, decimal price, Guid busId, Guid driverId)
        : base(id)
    {
        Direction = direction;
        DepartureTime = departureTime;
        Price = price;
        BusId = busId;
        DriverId = driverId;
        IsActive = true;
    }

    public static Result<Schedule> Create(Guid id, Direction direction, TimeOnly departureTime, decimal price, Guid busId, Guid driverId)
    {
        var validation = Validate(direction, price, busId, driverId);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new Schedule(id, direction, departureTime, price, busId, driverId);
    }

    public Result<Updated> Update(TimeOnly departureTime, decimal price, Guid busId, Guid driverId, bool isActive)
    {
        var validation = Validate(Direction, price, busId, driverId);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        DepartureTime = departureTime;
        Price = price;
        BusId = busId;
        DriverId = driverId;
        IsActive = isActive;

        return Result.Updated;
    }

    private static Result<Success> Validate(Direction direction, decimal price, Guid busId, Guid driverId)
    {
        if (!Enum.IsDefined(direction))
        {
            return ScheduleErrors.DirectionInvalid;
        }

        if (price <= 0)
        {
            return ScheduleErrors.PriceInvalid;
        }

        if (busId == Guid.Empty)
        {
            return ScheduleErrors.BusRequired;
        }

        if (driverId == Guid.Empty)
        {
            return ScheduleErrors.DriverRequired;
        }

        return Result.Success;
    }
}
