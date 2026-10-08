using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Application.Features.Schedules.Commands.CreateSchedule;
using SBus.Application.Features.Schedules.Commands.UpdateSchedule;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Application.Features.Schedules.Queries.GetSchedules;
using SBus.Domain.Routes;
using SBus.Web.Infrastructure;

namespace SBus.Web.Pages.Office.Settings;

public class SchedulesModel(ISender sender) : PageModel
{
    private readonly ISender _sender = sender;

    public List<ScheduleDto> Schedules { get; private set; } = [];

    public List<BusDto> Buses { get; private set; } = [];

    public List<DriverDto> Drivers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty]
    public Direction Direction { get; set; } = Direction.CairoToSuez;

    [BindProperty]
    public TimeOnly DepartureTime { get; set; } = new(9, 0);

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public Guid BusId { get; set; }

    [BindProperty]
    public Guid DriverId { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);

        if (Schedules.FirstOrDefault(s => s.ScheduleId == Id) is { } schedule)
        {
            Direction = schedule.Direction;
            DepartureTime = schedule.DepartureTime;
            Price = schedule.Price;
            BusId = schedule.BusId;
            DriverId = schedule.DriverId;
            IsActive = schedule.IsActive;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Id is Guid id)
        {
            var updated = await _sender.Send(new UpdateScheduleCommand(id, DepartureTime, Price, BusId, DriverId, IsActive), ct);

            if (updated.IsSuccess)
            {
                if (updated.Value.TripsKeptBecauseOfBookings > 0)
                {
                    this.SetSuccess(
                        "اتحفظ، واتعدلت {0} رحلة جاية. فيه {1} رحلة فيها حجوزات فضلت زي ما هي، عدّلها من كشف الرحلة لو محتاج.",
                        updated.Value.UpdatedTrips,
                        updated.Value.TripsKeptBecauseOfBookings);
                }
                else
                {
                    this.SetSuccess("اتحفظ، واتعدلت {0} رحلة جاية.", updated.Value.UpdatedTrips);
                }

                return RedirectToPage(new { id = (Guid?)null });
            }

            this.AddErrors(updated.Errors);
        }
        else
        {
            var created = await _sender.Send(new CreateScheduleCommand(Direction, DepartureTime, Price, BusId, DriverId), ct);

            if (created.IsSuccess)
            {
                this.SetSuccess("اتحفظ. الرحلات للأيام الجاية هتظهر خلال ساعة، أو أول ما تعيد تشغيل الموقع.");
                return RedirectToPage(new { id = (Guid?)null });
            }

            this.AddErrors(created.Errors);
        }

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var schedules = await _sender.Send(new GetSchedulesQuery(), ct);
        Schedules = schedules.IsSuccess ? schedules.Value : [];

        var buses = await _sender.Send(new GetBusesQuery(ActiveOnly: true), ct);
        Buses = buses.IsSuccess ? buses.Value : [];

        var drivers = await _sender.Send(new GetDriversQuery(ActiveOnly: true), ct);
        Drivers = drivers.IsSuccess ? drivers.Value : [];
    }
}
