using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages;

[EnableRateLimiting(RateLimitPolicies.PublicWrites)]
[Authorize(Policy = AuthPolicies.Traveler)]
[ServiceFilter(typeof(CompleteTravelerProfileFilter))]
public class TripModel(ISender sender, IUser user, ITravelerProfiles profiles, IStringLocalizer<SharedResource> localizer) : PageModel
{
    private readonly ISender _sender = sender;

    public TripBookingDto? Trip { get; private set; }
    public SBus.Application.Features.Accounts.Dtos.TravelerProfileDto? Profile { get; private set; }

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

        Input.PassengerName = Profile!.FullName;
        Input.PhoneNumber = Profile.PhoneNumber;
        return BookingPage();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return await BookingErrorsAsync(id, ct);
        }

        var result = await _sender.Send(
            new CreateOnlineBookingCommand(id, Input.PickupStopId, Input.DropoffStopId, Input.SeatNumbers,
                Input.EditPassengerDetails ? Input.PassengerName : null,
                Input.EditPassengerDetails ? Input.PhoneNumber : null,
                Input.EditPassengerDetails && Input.UpdateAccountDetails),
            ct);

        if (result.IsSuccess)
        {
            return Redirect($"/t/{result.Value.PublicToken}");
        }

        foreach (var error in result.Errors)
        {
            var key = error.Code is nameof(CreateOnlineBookingCommand.PassengerName) or nameof(CreateOnlineBookingCommand.PhoneNumber)
                ? $"Input.{error.Code}" : string.Empty;
            ModelState.AddModelError(key, localizer[error.Template, [.. error.Args]]);
        }

        return await BookingErrorsAsync(id, ct);
    }

    private async Task<IActionResult> BookingErrorsAsync(Guid id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct))
        {
            return NotFound();
        }

        return BookingPage();
    }

    private IActionResult BookingPage() =>
        PartialRequests.Prepare(HttpContext, "booking-content") ? Partial("_BookingForm", this) : Page();

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        Profile = await profiles.GetCurrentAsync(ct);
        var result = await _sender.Send(new GetTripForBookingQuery(id), ct);

        Trip = result.IsSuccess ? result.Value : null;

        return Trip is not null;
    }

    public class BookingInput
    {
        public bool EditPassengerDetails { get; set; }
        public bool EditPassengerName { get; set; }
        public bool EditPassengerPhone { get; set; }
        public string? PassengerName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool UpdateAccountDetails { get; set; }
        public Guid PickupStopId { get; set; }
        public Guid DropoffStopId { get; set; }
        public List<int> SeatNumbers { get; set; } = [];
    }
}
