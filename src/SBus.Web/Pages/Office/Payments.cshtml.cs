using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Bookings.Commands.ConfirmPayment;
using SBus.Application.Features.Bookings.Commands.RejectPayment;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetPendingPayments;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office;

public class PaymentsModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<PendingPaymentDto> Pending { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid bookingId, CancellationToken ct)
    {
        var result = await _sender.Send(new ConfirmPaymentCommand(bookingId), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("الحجز اتأكد.");
            return RedirectToPage();
        }

        this.AddErrors(result.Errors);
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid bookingId, string? reason, CancellationToken ct)
    {
        var result = await _sender.Send(new RejectPaymentCommand(bookingId, reason ?? string.Empty), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("التحويل اترفض والكراسي فضيت.");
            return RedirectToPage();
        }

        this.AddErrors(result.Errors);
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingPaymentsQuery(), ct);
        Pending = result.IsSuccess ? result.Value : [];
    }
}
