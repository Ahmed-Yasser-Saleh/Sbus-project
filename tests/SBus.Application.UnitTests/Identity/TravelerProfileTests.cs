using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Validation;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Application.UnitTests.Identity;

public class TravelerProfileTests
{
    [Theory]
    [InlineData("01012345678", "+201012345678")]
    [InlineData("+20 101-234-5678", "+201012345678")]
    [InlineData("00201012345678", "+201012345678")]
    [InlineData("٠١٠١٢٣٤٥٦٧٨", "+201012345678")]
    [InlineData("۰۱۰۱۲۳۴۵۶۷۸", "+201012345678")]
    [InlineData("12345", null)]
    public void ProfilePhone_NormalizesEgyptianMobileToE164(string input, string? expected)
    {
        Assert.Equal(expected, TravelerProfileRules.NormalizePhone(input));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("Google Name", null, false)]
    [InlineData(" ", "+201012345678", false)]
    [InlineData("Mona Ahmed", "+201012345678", true)]
    public void Completeness_RequiresBothNameAndPhone(string? name, string? phone, bool complete)
    {
        Assert.Equal(complete, new TravelerProfileDto(name, phone).IsComplete);
    }

    [Theory]
    [InlineData(null, "01012345678", "FullName")]
    [InlineData(" ", "01012345678", "FullName")]
    [InlineData("Mona Ahmed", null, "PhoneNumber")]
    [InlineData("Mona Ahmed", "12345", "PhoneNumber")]
    public void ProfileValidation_RejectsMissingOrInvalidData(string? name, string? phone, string field)
    {
        var edit = new UpdateTravelerProfileCommandValidator().Validate(new UpdateTravelerProfileCommand(name, phone));
        var register = new RegisterTravelerCommandValidator().Validate(new RegisterTravelerCommand("person@example.com", "abcde", "abcde", name, phone));
        Assert.Contains(edit.Errors, e => e.PropertyName == field);
        Assert.Contains(register.Errors, e => e.PropertyName == field);
    }

    [Fact]
    public void ProfileValidation_RejectsExcessiveLengths()
    {
        var result = new UpdateTravelerProfileCommandValidator().Validate(new UpdateTravelerProfileCommand(new string('a', 101), new string(' ', 40) + "01012345678"));
        Assert.Contains(result.Errors, e => e.PropertyName == "FullName");
        Assert.Contains(result.Errors, e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public async Task UpdateProfile_TrimsNameAndNormalizesPhone()
    {
        var profiles = Substitute.For<ITravelerProfiles>();
        profiles.UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<Success>>(Result.Success));
        var handler = new UpdateTravelerProfileCommandHandler(profiles);
        var result = await handler.Handle(new(" Mona Ahmed ", "01012345678"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        await profiles.Received(1).UpdateCurrentAsync("Mona Ahmed", "+201012345678", CancellationToken.None);
    }

    [Fact]
    public async Task OnlineBooking_IncompleteProfileFailsBeforeAccessingTripOrSaving()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns("traveler");
        user.IsTraveler.Returns(true);
        var profiles = Substitute.For<ITravelerProfiles>();
        profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Google Name", null));
        var db = Substitute.For<IAppDbContext>();
        var handler = new CreateOnlineBookingCommandHandler(
            Substitute.For<ILogger<CreateOnlineBookingCommandHandler>>(), db, TimeProvider.System,
            Options.Create(new BookingOptions()), user, profiles, Substitute.For<IBookingTransaction>());
        var result = await handler.Handle(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [1]), CancellationToken.None);
        Assert.True(result.IsError);
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _ = db.DidNotReceive().Trips;
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Family Member", "12345")]
    public async Task OnlineBooking_ValidatesResolvedPassengerBeforeTripAccess(string? name, string? phone)
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns("traveler");
        user.IsTraveler.Returns(true);
        var profiles = Substitute.For<ITravelerProfiles>();
        profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Profile Name", "bad phone"));
        var db = Substitute.For<IAppDbContext>();
        var transaction = Substitute.For<IBookingTransaction>();
        var handler = new CreateOnlineBookingCommandHandler(
            Substitute.For<ILogger<CreateOnlineBookingCommandHandler>>(), db, TimeProvider.System,
            Options.Create(new BookingOptions()), user, profiles, transaction);
        var result = await handler.Handle(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [1], name, phone, true), CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "PhoneNumber");
        _ = db.DidNotReceive().Trips;
        await profiles.DidNotReceive().UpdateCurrentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
