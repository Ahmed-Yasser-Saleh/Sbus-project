using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class LayoutsModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<SeatLayoutDto> Layouts { get; private set; } = [];

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Grid { get; set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new CreateSeatLayoutCommand(Name, Grid), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("اتحفظ ({0} كرسي).", result.Value.Capacity);
            return RedirectToPage();
        }

        this.AddErrors(result.Errors);
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetSeatLayoutsQuery(), ct);
        Layouts = result.IsSuccess ? result.Value : [];
    }
}
