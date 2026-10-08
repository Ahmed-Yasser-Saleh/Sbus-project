using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record TripSummaryDto(
    Guid TripId,
    Direction Direction,
    DateOnly ServiceDate,
    TimeOnly DepartureTime,
    decimal Price,
    int Capacity,
    int AvailableSeats);
