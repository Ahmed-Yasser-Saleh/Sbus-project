using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

using SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;
using SBus.Application.Features.Bookings.Commands.SubmitReceipt;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetTicket;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages;

[EnableRateLimiting(RateLimitPolicies.PublicWrites)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TicketModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public TicketDto? Ticket { get; private set; }

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken ct)
    {
        return await LoadAsync(token, ct) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostReceiptAsync(string token, IFormFile? receipt, CancellationToken ct)
    {
        if (receipt is null || receipt.Length == 0)
        {
            this.AddError("اختار صورة التحويل الأول.");
        }
        else
        {
            await using var stream = receipt.OpenReadStream();

            var result = await _sender.Send(new SubmitReceiptCommand(token, stream, receipt.Length), ct);

            if (result.IsSuccess)
            {
                this.SetSuccess("وصلتنا صورة التحويل. المكتب هيراجعها ويأكدلك الحجز.");
                return Redirect($"/t/{token}");
            }

            this.AddErrors(result.Errors);
        }

        return await LoadAsync(token, ct) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCancelAsync(string token, CancellationToken ct)
    {
        var result = await _sender.Send(new CancelBookingByPassengerCommand(token), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("الحجز اتلغى والكرسي بقى فاضي.");
            return Redirect($"/t/{token}");
        }

        this.AddErrors(result.Errors);

        return await LoadAsync(token, ct) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(string token, CancellationToken ct)
    {
        var result = await _sender.Send(new GetTicketQuery(token), ct);

        Ticket = result.IsSuccess ? result.Value : null;

        return Ticket is not null;
    }
}
