using SBus.Domain.Bookings;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Bookings.Dtos;

public sealed record TicketDto(
    Guid BookingId,
    string PublicToken,
    BookingStatus Status,
    BookingSource Source,
    string PassengerName,
    string PhoneNumber,
    IReadOnlyList<int> Seats,
    decimal PricePerSeat,
    decimal Amount,
    Direction Direction,
    DateOnly ServiceDate,
    TimeOnly DepartureTime,
    string PickupStopName,
    TimeOnly? EstimatedPickupTime,
    string DropoffStopName,
    DateTimeOffset? HoldExpiresAtUtc,
    string? DriverName,
    string? DriverPhoneNumber,
    string? RejectionReason,
    bool CanSubmitReceipt,
    bool CanCancel,
    DateTimeOffset CancellationDeadlineUtc,
    bool HasDeparted,
    string InstaPayAddress,
    string OfficePhone);
