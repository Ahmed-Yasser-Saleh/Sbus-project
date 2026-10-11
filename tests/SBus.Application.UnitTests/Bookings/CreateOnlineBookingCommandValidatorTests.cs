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

    [Theory]
    [InlineData("Family Member", "01012345678")]
    [InlineData("Family Member", "+201012345678")]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public void PassengerOverrides_ValidOrMissingValuesPass(string? name, string? phone)
    {
        Assert.True(_validator.Validate(Command() with { PassengerName = name, PhoneNumber = phone }).IsValid);
    }

    [Fact]
    public void PassengerOverrides_UseTheSameLimitsAsProfiles()
    {
        var result = _validator.Validate(Command() with { PassengerName = new string('x', 101), PhoneNumber = "12345" });
        Assert.Contains(result.Errors, e => e.PropertyName == "PassengerName");
        Assert.Contains(result.Errors, e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void BookingCommandLogging_DoesNotExposePassengerDetails()
    {
        var command = Command() with { PassengerName = "Family Member", PhoneNumber = "01012345678" };
        Assert.Equal(nameof(CreateOnlineBookingCommand), command.ToString());
    }

    private static CreateOnlineBookingCommand Command() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        [1, 2]);
}
