using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

using Xunit;

namespace SBus.Application.UnitTests.Bookings;

public class CreateOnlineBookingCommandValidatorTests
{
    private readonly CreateOnlineBookingCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(Command());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvalidPhone_Fails()
    {
        var result = _validator.Validate(Command() with { PhoneNumber = "12345" });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOnlineBookingCommand.PhoneNumber));
    }

    [Fact]
    public void SamePickupAndDropoff_Fails()
    {
        var stop = Guid.CreateVersion7();

        var result = _validator.Validate(Command() with { PickupStopId = stop, DropoffStopId = stop });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOnlineBookingCommand.DropoffStopId));
    }

    [Fact]
    public void FiveSeats_Fails()
    {
        var result = _validator.Validate(Command() with { SeatNumbers = [1, 2, 3, 4, 5] });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOnlineBookingCommand.SeatNumbers));
    }

    [Fact]
    public void DuplicateSeats_Fails()
    {
        var result = _validator.Validate(Command() with { SeatNumbers = [3, 3] });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOnlineBookingCommand.SeatNumbers));
    }

    private static CreateOnlineBookingCommand Command() => new(
        Guid.CreateVersion7(),
        "Mona",
        "01012345678",
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        [1, 2]);
}
