using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Bookings.Queries.GetReceipt;

namespace SBus.Web.Pages.Office;

public class ReceiptModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public async Task<IActionResult> OnGetAsync(Guid bookingId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetReceiptQuery(bookingId), ct);

        if (result.IsError)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";

        return File(result.Value.Content, result.Value.ContentType);
    }
}
