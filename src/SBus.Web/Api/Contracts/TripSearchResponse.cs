using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Routes;

namespace SBus.Web.Api.Contracts;

public sealed record TripSearchResponse(IReadOnlyList<TripSummaryDto> Data, PageMetadata Pagination,
    Direction Direction, DateOnly Date, IReadOnlyList<DateOnly> Days);
