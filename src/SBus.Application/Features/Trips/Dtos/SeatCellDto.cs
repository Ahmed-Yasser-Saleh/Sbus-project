namespace SBus.Application.Features.Trips.Dtos;

public sealed record SeatCellDto(int Row, int Column, SeatCellKind Kind, int? SeatNumber, bool IsTaken);
