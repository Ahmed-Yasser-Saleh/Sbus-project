namespace SBus.Application.Features.Drivers.Dtos;

public sealed record DriverDto(Guid DriverId, string Name, string PhoneNumber, bool IsActive);
