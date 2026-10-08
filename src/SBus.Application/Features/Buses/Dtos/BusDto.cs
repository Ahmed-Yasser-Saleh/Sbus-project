namespace SBus.Application.Features.Buses.Dtos;

public sealed record BusDto(Guid BusId, string PlateNumber, Guid SeatLayoutId, string SeatLayoutName, int Capacity, bool IsActive);
