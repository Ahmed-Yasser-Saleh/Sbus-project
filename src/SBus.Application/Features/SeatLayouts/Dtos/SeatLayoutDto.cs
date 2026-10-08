namespace SBus.Application.Features.SeatLayouts.Dtos;

public sealed record SeatLayoutDto(Guid SeatLayoutId, string Name, int Capacity, string Grid);
