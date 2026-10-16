using FluentValidation;

using Microsoft.Extensions.Localization;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Domain.Common.Results;
using SBus.Web;
using SBus.Web.Api.Errors;

using Xunit;

namespace SBus.Api.Tests;

public sealed class ApiResultAdapterTests
{
    public static IEnumerable<object[]> SettingsValidationErrors()
    {
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.NameRequired, "name"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.NameTooLong, "name"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.GridRequired, "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.TooManyRows, "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.TooManyColumns(1), "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.TooManySeats, "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.MultipleDrivers, "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.InvalidCell("?", 1), "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.DuplicateSeat(1), "grid"];
        yield return [SBus.Domain.Fleet.SeatLayoutErrors.NoSeats, "grid"];
        yield return [SBus.Domain.Routes.RouteStopErrors.DirectionInvalid, "direction"];
        yield return [SBus.Domain.Routes.RouteStopErrors.AtLeastTwoStops, "stops"];
        yield return [SBus.Domain.Routes.RouteStopErrors.StopRequired, "stops"];
        yield return [SBus.Domain.Routes.RouteStopErrors.DuplicateStop, "stops"];
        yield return [SBus.Domain.Routes.RouteStopErrors.FirstStopMustStartAtZero, "stops"];
        yield return [SBus.Domain.Routes.RouteStopErrors.MinutesMustIncrease, "stops"];
        yield return [SBus.Domain.Stops.StopErrors.NameRequired, "name"];
        yield return [SBus.Domain.Stops.StopErrors.NameTooLong, "name"];
        yield return [SBus.Domain.Fleet.BusErrors.PlateNumberRequired, "plateNumber"];
        yield return [SBus.Domain.Fleet.BusErrors.PlateNumberTooLong, "plateNumber"];
        yield return [SBus.Domain.Fleet.BusErrors.SeatLayoutRequired, "seatLayoutId"];
        yield return [SBus.Domain.Drivers.DriverErrors.NameRequired, "name"];
        yield return [SBus.Domain.Drivers.DriverErrors.NameTooLong, "name"];
        yield return [SBus.Domain.Drivers.DriverErrors.PhoneRequired, "phoneNumber"];
        yield return [SBus.Domain.Drivers.DriverErrors.PhoneTooLong, "phoneNumber"];
        yield return [SBus.Domain.Schedules.ScheduleErrors.DirectionInvalid, "direction"];
        yield return [SBus.Domain.Schedules.ScheduleErrors.PriceInvalid, "price"];
        yield return [SBus.Domain.Schedules.ScheduleErrors.BusRequired, "busId"];
        yield return [SBus.Domain.Schedules.ScheduleErrors.DriverRequired, "driverId"];
    }

    [Theory]
    [MemberData(nameof(SettingsValidationErrors))]
    public void SettingsValidation_UsesRequestFieldNames(Error error, string field)
    {
        var exception = Assert.Throws<ValidationException>(() => Create().Value<string>(error));
        Assert.Equal(field, Assert.Single(exception.Errors).PropertyName);
    }

    [Fact]
    public void Success_ReturnsSameDto()
    {
        Assert.Equal("value", Create().Value<string>("value"));
    }

    [Fact]
    public void ResultSeatConflict_UsesStableCode()
    {
        var exception = Assert.Throws<ConflictException>(() => Create().Value<string>(ApplicationErrors.SeatsJustTaken));
        Assert.Equal(ErrorCodes.BookingSeatUnavailable, exception.ErrorCode);
    }

    [Fact]
    public void ResultValidation_PreservesFieldPaths()
    {
        var exception = Assert.Throws<ValidationException>(() => Create().Value<string>(Error.Validation("PhoneNumber", "Invalid phone.")));
        Assert.Equal("phoneNumber", exception.Errors.Single().PropertyName);
    }

    [Theory]
    [InlineData(ErrorKind.NotFound, typeof(NotFoundException))]
    [InlineData(ErrorKind.Unauthorized, typeof(UnauthenticatedException))]
    [InlineData(ErrorKind.Forbidden, typeof(ForbiddenException))]
    [InlineData(ErrorKind.Failure, typeof(BusinessRuleException))]
    [InlineData(ErrorKind.Unexpected, typeof(InvalidOperationException))]
    public void OtherResultKinds_MapToExceptionHierarchy(ErrorKind kind, Type expected)
    {
        var exception = Record.Exception(() => Create().Value<string>(Error.Create((int)kind, "error", "Safe detail.")));
        Assert.IsType(expected, exception);
    }

    private static ApiResultAdapter Create()
    {
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call => new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));
        return new(localizer);
    }
}