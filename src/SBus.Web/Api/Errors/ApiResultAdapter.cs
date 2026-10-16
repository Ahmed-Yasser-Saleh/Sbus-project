using FluentValidation;
using FluentValidation.Results;

using Microsoft.Extensions.Localization;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Web.Api.Errors;

public sealed class ApiResultAdapter(IStringLocalizer<SharedResource> localizer)
{
    public T Value<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }

        var error = result.TopError;
        var detail = localizer[error.Template, [.. error.Args]].Value;
        var code = error.ReasonCode ?? ErrorCodes.ForDomainError(error.Code, error.Type);
        if (code == ErrorCodes.ProfileIncomplete)
        {
            throw new BusinessRuleException(detail, code);
        }

        if (error.Code == BookingErrors.HoldDurationInvalid.Code || error.Code == BookingErrors.PriceInvalid.Code)
        {
            throw new InvalidOperationException("Invalid booking configuration.");
        }

        if (error.Type == ErrorKind.Validation && (error.ReasonCode is not null || code != ErrorCodes.ValidationFailed))
        {
            var fields = result.Errors.GroupBy(item => ValidationField(item.Code))
                .ToDictionary(group => group.Key, group => group.Select(item => localizer[item.Template, [.. item.Args]].Value).ToArray());
            throw new RequestValidationException(fields, code);
        }

        throw error.Type switch
        {
            ErrorKind.Validation => new ValidationException(result.Errors.Select(item => new ValidationFailure(
                ValidationField(item.Code), localizer[item.Template, [.. item.Args]].Value))),
            ErrorKind.NotFound => new NotFoundException(detail, code),
            ErrorKind.Conflict => new ConflictException(detail, code),
            ErrorKind.Unauthorized => new UnauthenticatedException(detail, code),
            ErrorKind.Forbidden => new ForbiddenException(detail, code),
            ErrorKind.Failure => new BusinessRuleException(detail, code),
            _ => new InvalidOperationException("The application could not complete the request."),
        };
    }

    private static readonly IReadOnlyDictionary<string, string> ValidationFields = new Dictionary<string, string>
    {
        [ApplicationErrors.StopsNotFoundOrInactive.Code] = "stops",
        [SBus.Domain.Fleet.SeatLayoutErrors.NameRequired.Code] = "name",
        [SBus.Domain.Fleet.SeatLayoutErrors.NameTooLong.Code] = "name",
        [SBus.Domain.Fleet.SeatLayoutErrors.GridRequired.Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.TooManyRows.Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.TooManyColumns(1).Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.TooManySeats.Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.MultipleDrivers.Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.InvalidCell("?", 1).Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.DuplicateSeat(1).Code] = "grid",
        [SBus.Domain.Fleet.SeatLayoutErrors.NoSeats.Code] = "grid",
        [SBus.Domain.Routes.RouteStopErrors.DirectionInvalid.Code] = "direction",
        [SBus.Domain.Routes.RouteStopErrors.AtLeastTwoStops.Code] = "stops",
        [SBus.Domain.Routes.RouteStopErrors.StopRequired.Code] = "stops",
        [SBus.Domain.Routes.RouteStopErrors.DuplicateStop.Code] = "stops",
        [SBus.Domain.Routes.RouteStopErrors.FirstStopMustStartAtZero.Code] = "stops",
        [SBus.Domain.Routes.RouteStopErrors.MinutesMustIncrease.Code] = "stops",
        [SBus.Domain.Stops.StopErrors.NameRequired.Code] = "name",
        [SBus.Domain.Stops.StopErrors.NameTooLong.Code] = "name",
        [SBus.Domain.Fleet.BusErrors.PlateNumberRequired.Code] = "plateNumber",
        [SBus.Domain.Fleet.BusErrors.PlateNumberTooLong.Code] = "plateNumber",
        [SBus.Domain.Fleet.BusErrors.SeatLayoutRequired.Code] = "seatLayoutId",
        [SBus.Domain.Drivers.DriverErrors.NameRequired.Code] = "name",
        [SBus.Domain.Drivers.DriverErrors.NameTooLong.Code] = "name",
        [SBus.Domain.Drivers.DriverErrors.PhoneRequired.Code] = "phoneNumber",
        [SBus.Domain.Drivers.DriverErrors.PhoneTooLong.Code] = "phoneNumber",
        [SBus.Domain.Schedules.ScheduleErrors.DirectionInvalid.Code] = "direction",
        [SBus.Domain.Schedules.ScheduleErrors.PriceInvalid.Code] = "price",
        [SBus.Domain.Schedules.ScheduleErrors.BusRequired.Code] = "busId",
        [SBus.Domain.Schedules.ScheduleErrors.DriverRequired.Code] = "driverId",
        [ApplicationErrors.StopsNotOnRoute.Code] = "pickupStopId",
        [ApplicationErrors.PickupAfterDropoff.Code] = "dropoffStopId",
        [ApplicationErrors.SeatsNotInLayout([]).Code] = "seatNumbers",
        [SBus.Application.Features.Bookings.Common.ReceiptErrorKeys.TooLarge] = "receipt",
        [SBus.Application.Features.Bookings.Common.ReceiptErrorKeys.UnsupportedFile] = "receipt",
        ["Content"] = "receipt",
        ["Length"] = "receipt",
        ["PublicToken"] = "token",
        [BookingErrors.TripRequired.Code] = "tripId",
        [BookingErrors.PassengerNameRequired.Code] = "passengerName",
        [BookingErrors.PassengerNameTooLong.Code] = "passengerName",
        [BookingErrors.PhoneRequired.Code] = "phoneNumber",
        [BookingErrors.PhoneTooLong.Code] = "phoneNumber",
        [BookingErrors.StopsRequired.Code] = "dropoffStopId",
        [BookingErrors.SameStop.Code] = "dropoffStopId",
        [BookingErrors.SeatsRequired.Code] = "seatNumbers",
        [BookingErrors.TooManySeats.Code] = "seatNumbers",
        [BookingErrors.SeatNumberInvalid.Code] = "seatNumbers",
        [BookingErrors.DuplicateSeats.Code] = "seatNumbers",
    };

    private static string ValidationField(string code) => ValidationFields.TryGetValue(code, out var field) ? field : ApiExceptionHandler.JsonField(code);
}