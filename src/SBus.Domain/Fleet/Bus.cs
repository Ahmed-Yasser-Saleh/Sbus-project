using SBus.Domain.Common;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Fleet;

public sealed class Bus : AuditableEntity
{
    public string PlateNumber { get; private set; } = null!;
    public Guid SeatLayoutId { get; private set; }
    public SeatLayout? SeatLayout { get; private set; }
    public bool IsActive { get; private set; }

    private Bus()
    { }

    private Bus(Guid id, string plateNumber, Guid seatLayoutId)
        : base(id)
    {
        PlateNumber = plateNumber;
        SeatLayoutId = seatLayoutId;
        IsActive = true;
    }

    public static Result<Bus> Create(Guid id, string plateNumber, Guid seatLayoutId)
    {
        var validation = Validate(plateNumber, seatLayoutId);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new Bus(id, plateNumber.Trim(), seatLayoutId);
    }

    public Result<Updated> Update(string plateNumber, Guid seatLayoutId, bool isActive)
    {
        var validation = Validate(plateNumber, seatLayoutId);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        PlateNumber = plateNumber.Trim();
        SeatLayoutId = seatLayoutId;
        IsActive = isActive;

        return Result.Updated;
    }

    private static Result<Success> Validate(string plateNumber, Guid seatLayoutId)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            return BusErrors.PlateNumberRequired;
        }

        if (plateNumber.Trim().Length > SBusConstants.NameMaxLength)
        {
            return BusErrors.PlateNumberTooLong;
        }

        if (seatLayoutId == Guid.Empty)
        {
            return BusErrors.SeatLayoutRequired;
        }

        return Result.Success;
    }
}
