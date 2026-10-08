using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Commands.UpdateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Queries.GetStops;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class StopsModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<StopDto> Stops { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        if (Stops.FirstOrDefault(s => s.StopId == Id) is { } stop)
        {
            Name = stop.Name;
            Description = stop.Description;
            IsActive = stop.IsActive;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var errors = Id is Guid id
            ? (await _sender.Send(new UpdateStopCommand(id, Name, Description, IsActive), ct)).Errors
            : (await _sender.Send(new CreateStopCommand(Name, Description), ct)).Errors;

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
        var result = await _sender.Send(new GetStopsQuery(), ct);
        Stops = result.IsSuccess ? result.Value : [];
    }
}
