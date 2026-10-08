using SBus.Domain.Common;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Stops;

public sealed class Stop : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Stop()
    { }

    private Stop(Guid id, string name, string? description)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public static Result<Stop> Create(Guid id, string name, string? description)
    {
        var validation = Validate(name);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new Stop(id, name.Trim(), Normalize(description));
    }

    public Result<Updated> Update(string name, string? description, bool isActive)
    {
        var validation = Validate(name);

        if (validation.IsError)
        {
            return validation.Errors;
        }

        Name = name.Trim();
        Description = Normalize(description);
        IsActive = isActive;

        return Result.Updated;
    }

    private static Result<Success> Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StopErrors.NameRequired;
        }

        if (name.Trim().Length > SBusConstants.NameMaxLength)
        {
            return StopErrors.NameTooLong;
        }

        return Result.Success;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
