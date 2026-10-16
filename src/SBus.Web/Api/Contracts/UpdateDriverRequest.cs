namespace SBus.Web.Api.Contracts;

public sealed record UpdateDriverRequest(string Name, string PhoneNumber, bool IsActive);

