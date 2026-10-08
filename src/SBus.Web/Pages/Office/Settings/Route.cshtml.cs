using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Routes.Commands.SetRouteStops;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Queries.GetStops;
using SBus.Domain.Routes;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class RouteModel(ISender sender) : PageModel
{
    public const int RowCount = 10;

    private readonly ISender _sender = sender;

    [BindProperty(SupportsGet = true)]
    public Direction Direction { get; set; } = Direction.CairoToSuez;

    [BindProperty]
    public List<RouteRow> Rows { get; set; } = [];

    public List<StopDto> Stops { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadStopsAsync(ct);

        var route = await _sender.Send(new GetRouteQuery(Direction), ct);

        Rows = route.IsSuccess
            ? [.. route.Value.Select(r => new RouteRow { StopId = r.StopId, MinutesFromStart = r.MinutesFromStart })]
            : [];

        PadRows();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var items = Rows
            .Where(r => r.StopId is Guid id && id != Guid.Empty)
            .Select(r => new RouteStopItem(r.StopId!.Value, r.MinutesFromStart ?? 0))
            .ToList();

        var result = await _sender.Send(new SetRouteStopsCommand(Direction, items), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("الخط اتحفظ.");
            return RedirectToPage(new { direction = Direction });
        }

        this.AddErrors(result.Errors);
        await LoadStopsAsync(ct);
        PadRows();
        return Page();
    }

    private async Task LoadStopsAsync(CancellationToken ct)
    {
        var stops = await _sender.Send(new GetStopsQuery(ActiveOnly: true), ct);
        Stops = stops.IsSuccess ? stops.Value : [];
    }

    private void PadRows()
    {
        while (Rows.Count < RowCount)
        {
            Rows.Add(new RouteRow());
        }
    }

    public class RouteRow
    {
        public Guid? StopId { get; set; }
        public int? MinutesFromStart { get; set; }
    }
}
