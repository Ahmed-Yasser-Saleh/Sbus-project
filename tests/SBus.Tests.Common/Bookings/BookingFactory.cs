using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Tests.Common.Bookings;

public static class BookingFactory
{
    public static readonly DateTimeOffset Now = new(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);

    public static Result<Booking> CreateHold(
        Guid? tripId = null,
        string token = "token-0123456789",
        string name = "Mona",
        string phone = "01012345678",
        Guid? pickupStopId = null,
        Guid? dropoffStopId = null,
        IReadOnlyCollection<int>? seats = null,
        decimal pricePerSeat = 150,
        DateTimeOffset? nowUtc = null,
        TimeSpan? holdDuration = null)
    {
        return Booking.CreateHold(
            Guid.CreateVersion7(),
            tripId ?? Guid.CreateVersion7(),
            token,
            name,
            phone,
            pickupStopId ?? Guid.CreateVersion7(),
            dropoffStopId ?? Guid.CreateVersion7(),
            seats ?? [1, 2],
            pricePerSeat,
            nowUtc ?? Now,
            holdDuration ?? TimeSpan.FromMinutes(15));
    }

    public static Result<Booking> CreateByOffice(IReadOnlyCollection<int>? seats = null, DateTimeOffset? nowUtc = null)
    {
        return Booking.CreateByOffice(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "office-token-0123456789",
            "Karim",
            "01112345678",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            seats ?? [5],
            150,
            nowUtc ?? Now);
    }
}
