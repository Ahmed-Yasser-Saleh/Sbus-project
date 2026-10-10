using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Common.Security;
using SBus.Application.Features.Bookings.Queries.GetMyTickets;

namespace SBus.Web.Pages;

[Authorize(Policy = AuthPolicies.Traveler)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MyTicketsModel(ISender sender) : PageModel
{
    public IReadOnlyList<MyTicketDto> Tickets { get; private set; } = [];
    public int PageNumber { get; private set; }

    public async Task OnGetAsync(int pageNumber = 1, CancellationToken ct = default)
    {
        PageNumber = Math.Clamp(pageNumber, 1, 10000);
        Tickets = await sender.Send(new GetMyTicketsQuery(PageNumber), ct);
    }
}

