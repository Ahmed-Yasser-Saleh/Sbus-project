using SBus.Application.Features.Routes.Dtos;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record TripBookingDto(
    Guid TripId,
    Direction Direction,
    DateOnly ServiceDate,
    TimeOnly DepartureTime,
    decimal Price,
    bool IsOpenForBooking,
    SeatMapDto SeatMap,
    List<RouteStopDto> Stops);
