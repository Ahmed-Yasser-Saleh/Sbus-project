using MediatR;

using Microsoft.Extensions.Options;

using SBus.Application.Common.Settings;
using SBus.Application.Common.Time;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetAvailableTrips;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Trips.Queries.SearchTrips;

public sealed class SearchTripsQueryHandler(ISender sender, TimeProvider clock, IOptions<BookingOptions> options)
    : IRequestHandler<SearchTripsQuery, Result<TripSearchDto>>
{
    public async Task<Result<TripSearchDto>> Handle(SearchTripsQuery request, CancellationToken ct)
    {
        var today = CairoTime.Today(clock.GetUtcNow());
        var days = Enumerable.Range(0, options.Value.BookingWindowDays).Select(today.AddDays).ToList();
        var date = request.Date is { } selected && days.Contains(selected) ? selected : today;
        var direction = Enum.IsDefined(request.Direction) ? request.Direction : Direction.CairoToSuez;
        var trips = await sender.Send(new GetAvailableTripsQuery(direction, date), ct);
        if (trips.IsError)
        {
            return trips.Errors;
        }

        var offset = (request.Page - 1) * request.PageSize;
        return new TripSearchDto(direction, date, days, trips.Value.Skip(offset).Take(request.PageSize).ToList(),
            request.Page, request.PageSize, trips.Value.Count > offset + request.PageSize);
    }
}