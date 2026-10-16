namespace SBus.Application.Common.Errors;

public static class ErrorCodes
{
    public const string ProfileIncomplete = "TRAVELER_PROFILE_INCOMPLETE";
    public const string ProfileUpdateFailed = "PROFILE_UPDATE_FAILED";
    public const string ReceiptTooLarge = "RECEIPT_TOO_LARGE";
    public const string ReceiptUnsupportedFile = "RECEIPT_UNSUPPORTED_FILE";
    public const string BookingStopsNotOnRoute = "BOOKING_STOPS_NOT_ON_ROUTE";
    public const string BookingStopOrderInvalid = "BOOKING_STOP_ORDER_INVALID";
    public const string BookingSeatsNotInLayout = "BOOKING_SEATS_NOT_IN_LAYOUT";
    public const string EmailAlreadyRegistered = "AUTH_EMAIL_ALREADY_REGISTERED";
    public const string RegistrationFailed = "AUTH_REGISTRATION_FAILED";
    public const string ConfirmationInvalid = "AUTH_CONFIRMATION_INVALID";
    public const string PasswordResetInvalid = "AUTH_PASSWORD_RESET_INVALID";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthenticated = "AUTH_UNAUTHENTICATED";
    public const string Forbidden = "AUTH_FORBIDDEN";
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "BUSINESS_CONFLICT";
    public const string BusinessRule = "BUSINESS_RULE_VIOLATION";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ApiUnavailable = "API_UNAVAILABLE";
    public const string RouteNotFound = "API_ROUTE_NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string RequestTooLarge = "REQUEST_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string EmailConfirmationRequired = "AUTH_EMAIL_CONFIRMATION_REQUIRED";
    public const string RefreshInvalid = "AUTH_REFRESH_INVALID";
    public const string GoogleTokenInvalid = "AUTH_GOOGLE_TOKEN_INVALID";
    public const string GoogleSignInFailed = "AUTH_GOOGLE_SIGN_IN_FAILED";
    public const string GoogleLinkInvalid = "AUTH_GOOGLE_LINK_INVALID";
    public const string GoogleLinkFailed = "AUTH_GOOGLE_LINK_FAILED";
    public const string GoogleUnavailable = "AUTH_GOOGLE_UNAVAILABLE";
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string TripNotFound = "TRIP_NOT_FOUND";
    public const string BookingSeatUnavailable = "BOOKING_SEAT_UNAVAILABLE";
    public const string TripDeparted = "TRIP_DEPARTED";
    public const string TripNotBookable = "TRIP_NOT_BOOKABLE";
    public const string BookingReceiptNotAllowed = "BOOKING_RECEIPT_NOT_ALLOWED";
    public const string BookingCancellationClosed = "BOOKING_CANCELLATION_CLOSED";
    public const string BookingInvalidTransition = "BOOKING_INVALID_TRANSITION";

    public static string ForDomainError(string code, SBus.Domain.Common.Results.ErrorKind kind)
    {
        if (code == ApplicationErrors.StopsNotOnRoute.Code) { return BookingStopsNotOnRoute; }
        if (code == ApplicationErrors.PickupAfterDropoff.Code) { return BookingStopOrderInvalid; }
        if (code == ApplicationErrors.SeatsNotInLayout([]).Code) { return BookingSeatsNotInLayout; }
        if (code == SBus.Application.Features.Bookings.Common.ReceiptErrorKeys.TooLarge) { return ReceiptTooLarge; }
        if (code == SBus.Application.Features.Bookings.Common.ReceiptErrorKeys.UnsupportedFile) { return ReceiptUnsupportedFile; }
        if (code == ApplicationErrors.TripNotFound.Code)
        {
            return TripNotFound;
        }

        if (code == ApplicationErrors.BookingNotFound.Code)
        {
            return BookingNotFound;
        }

        if (code == ApplicationErrors.SeatsJustTaken.Code || code == ApplicationErrors.SeatsTaken([]).Code)
        {
            return BookingSeatUnavailable;
        }

        if (code == ApplicationErrors.TripDeparted.Code)
        {
            return TripDeparted;
        }

        if (code == ApplicationErrors.TripNotBookable.Code)
        {
            return TripNotBookable;
        }

        if (code == SBus.Domain.Bookings.BookingErrors.ReceiptNotAllowed(default).Code)
        {
            return BookingReceiptNotAllowed;
        }

        if (code == SBus.Domain.Bookings.BookingErrors.PassengerCancellationClosed(default).Code)
        {
            return BookingCancellationClosed;
        }

        if (code == SBus.Domain.Bookings.BookingErrors.InvalidTransition(default, default).Code)
        {
            return BookingInvalidTransition;
        }

        return kind switch
        {
            SBus.Domain.Common.Results.ErrorKind.Validation => ValidationFailed,
            SBus.Domain.Common.Results.ErrorKind.NotFound => NotFound,
            SBus.Domain.Common.Results.ErrorKind.Conflict => Conflict,
            SBus.Domain.Common.Results.ErrorKind.Unauthorized => Unauthenticated,
            SBus.Domain.Common.Results.ErrorKind.Forbidden => Forbidden,
            SBus.Domain.Common.Results.ErrorKind.Failure => BusinessRule,
            _ => InternalError,
        };
    }
}
