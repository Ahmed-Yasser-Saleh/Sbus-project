namespace SBus.Web.Api.Contracts;

public sealed record TripAssignmentRequest(Guid BusId, Guid DriverId);

