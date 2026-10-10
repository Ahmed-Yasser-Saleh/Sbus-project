using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages;

[EnableRateLimiting(RateLimitPolicies.PublicWrites)]
[Authorize(Policy = AuthPolicies.Traveler)]
public class TripModel(ISender sender, IUser user) : PageModel
{
    private readonly ISender _sender = sender;

    public TripBookingDto? Trip { get; private set; }

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct))
        {
            return NotFound();
        }

        Input.PickupStopId = Trip!.Stops.FirstOrDefault()?.StopId ?? Guid.Empty;
        Input.DropoffStopId = Trip.Stops.LastOrDefault()?.StopId ?? Guid.Empty;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return Forbid();
        }

        var result = await _sender.Send(
            new CreateOnlineBookingCommand(id, Input.PassengerName, Input.PhoneNumber, Input.PickupStopId, Input.DropoffStopId, Input.SeatNumbers),
            ct);

        if (result.IsSuccess)
        {
            return Redirect($"/t/{result.Value.PublicToken}");
        }

        this.AddErrors(result.Errors);

        if (!await LoadAsync(id, ct))
        {
            return NotFound();
        }

        return Page();
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetTripForBookingQuery(id), ct);

        Trip = result.IsSuccess ? result.Value : null;

        return Trip is not null;
    }

    public class BookingInput
    {
        public string PassengerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid PickupStopId { get; set; }
        public Guid DropoffStopId { get; set; }
        public List<int> SeatNumbers { get; set; } = [];
    }
}
