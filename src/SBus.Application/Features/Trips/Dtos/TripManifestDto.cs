using SBus.Application.Features.Routes.Dtos;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record TripManifestDto(
    Guid TripId,
    Direction Direction,
    DateOnly ServiceDate,
    TimeOnly DepartureTime,
    decimal Price,
    Guid BusId,
    string BusPlateNumber,
    Guid DriverId,
    string DriverName,
    string DriverPhoneNumber,
    bool HasDeparted,
    SeatMapDto SeatMap,
    List<RouteStopDto> Stops,
    List<ManifestBookingDto> Bookings);
