namespace SBus.Web.Api.Contracts;

public sealed record OfficeBookingRequest(string PassengerName, string PhoneNumber, Guid PickupStopId, Guid DropoffStopId, List<int> SeatNumbers);

