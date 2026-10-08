using SBus.Domain.Routes;

namespace SBus.Application.Features.Bookings.Dtos;

public sealed record PendingPaymentDto(
    Guid BookingId,
    Guid TripId,
    string PassengerName,
    string PhoneNumber,
    IReadOnlyList<int> Seats,
    decimal Amount,
    Direction Direction,
    DateOnly ServiceDate,
    TimeOnly DepartureTime,
    DateTimeOffset? ReceiptSubmittedAtUtc);
