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
        if (error.Type == ErrorKind.Validation && error.ReasonCode is not null)
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

    private static readonly IReadOnlyDictionary<string, string> BookingFields = new Dictionary<string, string>
    {
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

    private static string ValidationField(string code) => BookingFields.TryGetValue(code, out var field) ? field : ApiExceptionHandler.JsonField(code);
}
