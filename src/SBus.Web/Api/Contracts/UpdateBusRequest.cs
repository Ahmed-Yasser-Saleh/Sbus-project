namespace SBus.Web.Api.Contracts;

public sealed record UpdateBusRequest(string PlateNumber, Guid SeatLayoutId, bool IsActive);

