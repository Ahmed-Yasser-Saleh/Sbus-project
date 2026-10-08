using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Buses.Commands.CreateBus;
using SBus.Application.Features.Buses.Commands.UpdateBus;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class BusesModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<BusDto> Buses { get; private set; } = [];

    public List<SeatLayoutDto> Layouts { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty]
    public string PlateNumber { get; set; } = string.Empty;

    [BindProperty]
    public Guid SeatLayoutId { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        if (Buses.FirstOrDefault(b => b.BusId == Id) is { } bus)
        {
            PlateNumber = bus.PlateNumber;
            SeatLayoutId = bus.SeatLayoutId;
            IsActive = bus.IsActive;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var errors = Id is Guid id
            ? (await _sender.Send(new UpdateBusCommand(id, PlateNumber, SeatLayoutId, IsActive), ct)).Errors
            : (await _sender.Send(new CreateBusCommand(PlateNumber, SeatLayoutId), ct)).Errors;

        if (errors.Count == 0)
        {
            this.SetSuccess("اتحفظ.");
            return RedirectToPage(new { id = (Guid?)null });
        }

        this.AddErrors(errors);
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var buses = await _sender.Send(new GetBusesQuery(), ct);
        Buses = buses.IsSuccess ? buses.Value : [];

        var layouts = await _sender.Send(new GetSeatLayoutsQuery(), ct);
        Layouts = layouts.IsSuccess ? layouts.Value : [];
    }
}
