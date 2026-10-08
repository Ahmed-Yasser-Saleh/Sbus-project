using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Bookings.Commands.CancelBookingByOffice;
using SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Application.Features.Trips.Commands.UpdateTripAssignment;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetTripManifest;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office;

public class TripModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public TripManifestDto? Trip { get; private set; }

    public List<BusDto> Buses { get; private set; } = [];

    public List<DriverDto> Drivers { get; private set; } = [];

    [BindProperty]
    public OfficeBookingInput Input { get; set; } = new();

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

    public async Task<IActionResult> OnPostBookAsync(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(
            new CreateOfficeBookingCommand(id, Input.PassengerName, Input.PhoneNumber, Input.PickupStopId, Input.DropoffStopId, Input.SeatNumbers),
            ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("اتحجز. لينك التذكرة للراكب: {0}", $"{Request.Scheme}://{Request.Host}/t/{result.Value.PublicToken}");
            return RedirectToPage(new { id });
        }

        this.AddErrors(result.Errors);
        return await LoadAsync(id, ct) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, Guid bookingId, CancellationToken ct)
    {
        var result = await _sender.Send(new CancelBookingByOfficeCommand(bookingId), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("الحجز اتلغى والكراسي فضيت.");
            return RedirectToPage(new { id });
        }

        this.AddErrors(result.Errors);
        return await LoadAsync(id, ct) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, Guid busId, Guid driverId, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateTripAssignmentCommand(id, busId, driverId), ct);

        if (result.IsSuccess)
        {
            this.SetSuccess("الأتوبيس والسواق اتحدثوا للرحلة دي.");
            return RedirectToPage(new { id });
        }

        this.AddErrors(result.Errors);
        return await LoadAsync(id, ct) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        var trip = await _sender.Send(new GetTripManifestQuery(id), ct);
        Trip = trip.IsSuccess ? trip.Value : null;

        var buses = await _sender.Send(new GetBusesQuery(ActiveOnly: true), ct);
        Buses = buses.IsSuccess ? buses.Value : [];

        var drivers = await _sender.Send(new GetDriversQuery(ActiveOnly: true), ct);
        Drivers = drivers.IsSuccess ? drivers.Value : [];

        return Trip is not null;
    }

    public class OfficeBookingInput
    {
        public string PassengerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid PickupStopId { get; set; }
        public Guid DropoffStopId { get; set; }
        public List<int> SeatNumbers { get; set; } = [];
    }
}
