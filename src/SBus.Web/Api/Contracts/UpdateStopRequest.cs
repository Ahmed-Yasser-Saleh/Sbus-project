namespace SBus.Web.Api.Contracts;

public sealed record UpdateStopRequest(string Name, string? Description, bool IsActive);

