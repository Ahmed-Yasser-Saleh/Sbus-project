using SBus.Domain.Common;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Drivers;

public sealed class Driver : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Driver()
    { }

    private Driver(Guid id, string name, string phoneNumber)
        : base(id)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        IsActive = true;
    }

    public static Result<Driver> Create(Guid id, string name, string phoneNumber)
    {
        var validation = Validate(name, phoneNumber);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new Driver(id, name.Trim(), phoneNumber.Trim());
    }

    public Result<Updated> Update(string name, string phoneNumber, bool isActive)
    {
        var validation = Validate(name, phoneNumber);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        Name = name.Trim();
        PhoneNumber = phoneNumber.Trim();
        IsActive = isActive;

        return Result.Updated;
    }

    private static Result<Success> Validate(string name, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DriverErrors.NameRequired;
        }

        if (name.Trim().Length > SBusConstants.NameMaxLength)
        {
            return DriverErrors.NameTooLong;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return DriverErrors.PhoneRequired;
        }

        if (phoneNumber.Trim().Length > SBusConstants.PhoneMaxLength)
        {
            return DriverErrors.PhoneTooLong;
        }

        return Result.Success;
    }
}
