using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record OfficeTripDto(
    Guid TripId,
    Direction Direction,
    TimeOnly DepartureTime,
    string BusPlateNumber,
    string DriverName,
    int Capacity,
    int ConfirmedSeats,
    int AwaitingSeats,
    int HeldSeats);
