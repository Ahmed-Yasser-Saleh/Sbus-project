using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Common.Time;
using SBus.Application.Features.Bookings.Queries.GetPendingPayments;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetOfficeDay;

namespace SBus.Web.Pages.Office;

public class IndexModel(ISender sender, TimeProvider timeProvider) : PageModel
{
    private readonly ISender _sender = sender;
    private readonly TimeProvider _timeProvider = timeProvider;

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    public DateOnly Today { get; private set; }

    public List<OfficeTripDto> Trips { get; private set; } = [];

    public int PendingPayments { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Today = CairoTime.Today(_timeProvider.GetUtcNow());
        Date ??= Today;

        var trips = await _sender.Send(new GetOfficeDayQuery(Date.Value), ct);
        Trips = trips.IsSuccess ? trips.Value : [];

        var pending = await _sender.Send(new GetPendingPaymentsQuery(), ct);
        PendingPayments = pending.IsSuccess ? pending.Value.Count : 0;
    }
}
