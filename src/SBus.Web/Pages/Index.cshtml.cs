using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Settings;
using SBus.Application.Common.Time;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetAvailableTrips;
using SBus.Domain.Routes;

namespace SBus.Web.Pages;

public class IndexModel(ISender sender, TimeProvider timeProvider, IOptions<BookingOptions> options) : PageModel
{
    private readonly ISender _sender = sender;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    [BindProperty(SupportsGet = true)]
    public Direction Direction { get; set; } = Direction.CairoToSuez;

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    public List<DateOnly> Days { get; private set; } = [];

    public List<TripSummaryDto> Trips { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var today = CairoTime.Today(_timeProvider.GetUtcNow());

        Days = [.. Enumerable.Range(0, _options.BookingWindowDays).Select(today.AddDays)];

        if (Date is null || !Days.Contains(Date.Value))
        {
            Date = today;
        }

        if (!Enum.IsDefined(Direction))
        {
            Direction = Direction.CairoToSuez;
        }

        var result = await _sender.Send(new GetAvailableTripsQuery(Direction, Date.Value), ct);

        Trips = result.IsSuccess ? result.Value : [];
    }
}
