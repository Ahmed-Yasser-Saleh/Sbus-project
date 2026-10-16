using SBus.Application.Features.Trips.Dtos;

namespace SBus.Web.Api.Contracts;

public sealed record OfficeDayResponse(DateOnly Date, DateOnly Today, List<OfficeTripDto> Trips, int PendingPayments);

