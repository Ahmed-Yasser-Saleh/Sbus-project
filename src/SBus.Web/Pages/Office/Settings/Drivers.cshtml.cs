using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Drivers.Commands.CreateDriver;
using SBus.Application.Features.Drivers.Commands.UpdateDriver;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class DriversModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<DriverDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        if (Drivers.FirstOrDefault(d => d.DriverId == Id) is { } driver)
        {
            Name = driver.Name;
            PhoneNumber = driver.PhoneNumber;
            IsActive = driver.IsActive;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var errors = Id is Guid id
            ? (await _sender.Send(new UpdateDriverCommand(id, Name, PhoneNumber, IsActive), ct)).Errors
            : (await _sender.Send(new CreateDriverCommand(Name, PhoneNumber), ct)).Errors;

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
        var result = await _sender.Send(new GetDriversQuery(), ct);
        Drivers = result.IsSuccess ? result.Value : [];
    }
}
