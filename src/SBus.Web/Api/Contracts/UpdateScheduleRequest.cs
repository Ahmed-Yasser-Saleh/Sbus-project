namespace SBus.Web.Api.Contracts;

public sealed record UpdateScheduleRequest(TimeOnly DepartureTime, decimal Price, Guid BusId, Guid DriverId, bool IsActive);

