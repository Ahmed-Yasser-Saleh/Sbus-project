using SBus.Application.Common.Time;
using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;
using SBus.Domain.Schedules;
using SBus.Domain.Stops;
using SBus.Domain.Trips;
using SBus.Infrastructure.Data;
using SBus.Tests.Common.Fleet;

namespace SBus.Application.SubcutaneousTests.Common;

public sealed class TestWorld
{
    private static int _nextServiceDay;

    private TestWorld(Guid first, Guid middle, Guid last, Guid busId, Guid driverId, Guid scheduleId)
    {
        FirstStopId = first;
        MiddleStopId = middle;
        LastStopId = last;
        BusId = busId;
        DriverId = driverId;
        ScheduleId = scheduleId;
    }

    public const string DriverPhone = "01099999999";

    public Guid FirstStopId { get; }
    public Guid MiddleStopId { get; }
    public Guid LastStopId { get; }
    public Guid BusId { get; }
    public Guid DriverId { get; }
    public Guid ScheduleId { get; }

    public static async Task<TestWorld> CreateAsync(AppDbContext context)
    {
        var first = Stop.Create(Guid.CreateVersion7(), "Test Start", null).Value;
        var middle = Stop.Create(Guid.CreateVersion7(), "Test Middle", null).Value;
        var last = Stop.Create(Guid.CreateVersion7(), "Test End", null).Value;
        context.Stops.AddRange(first, middle, last);

        context.RouteStops.AddRange(RouteStop.CreateSequence(Direction.CairoToSuez, [(first.Id, 0), (middle.Id, 30), (last.Id, 120)]).Value);

        var layout = SeatLayoutFactory.MiniBus("Test mini bus");
        context.SeatLayouts.Add(layout);

        var bus = Bus.Create(Guid.CreateVersion7(), "TEST 1", layout.Id).Value;
        context.Buses.Add(bus);

        var driver = Driver.Create(Guid.CreateVersion7(), "Test Driver", DriverPhone).Value;
        context.Drivers.Add(driver);

        var schedule = Schedule.Create(Guid.CreateVersion7(), Direction.CairoToSuez, new TimeOnly(9, 0), 150, bus.Id, driver.Id).Value;
        context.Schedules.Add(schedule);

        await context.SaveChangesAsync();

        return new TestWorld(first.Id, middle.Id, last.Id, bus.Id, driver.Id, schedule.Id);
    }

    public async Task<Guid> CreateTripAsync(AppDbContext context, DateTimeOffset departureAtUtc)
    {
        var serviceDate = new DateOnly(2040, 1, 1).AddDays(Interlocked.Increment(ref _nextServiceDay));

        var trip = Trip.Create(
            Guid.CreateVersion7(),
            ScheduleId,
            Direction.CairoToSuez,
            serviceDate,
            TimeOnly.FromDateTime(CairoTime.ToLocal(departureAtUtc)),
            departureAtUtc,
            150,
            BusId,
            DriverId).Value;

        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        return trip.Id;
    }
}
