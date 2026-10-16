using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Dtos;

public sealed record TripSearchDto(Direction Direction, DateOnly Date, IReadOnlyList<DateOnly> Days,
    IReadOnlyList<TripSummaryDto> Trips, int Page, int PageSize, bool HasMore);
