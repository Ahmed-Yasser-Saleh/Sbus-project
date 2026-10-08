using Microsoft.EntityFrameworkCore;

using SBus.Domain.Bookings;
using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;
using SBus.Domain.Schedules;
using SBus.Domain.Stops;
using SBus.Domain.Trips;

namespace SBus.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<Stop> Stops { get; }
    public DbSet<RouteStop> RouteStops { get; }
    public DbSet<SeatLayout> SeatLayouts { get; }
    public DbSet<Bus> Buses { get; }
    public DbSet<Driver> Drivers { get; }
    public DbSet<Schedule> Schedules { get; }
    public DbSet<Trip> Trips { get; }
    public DbSet<Booking> Bookings { get; }
    public DbSet<BookingSeat> BookingSeats { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
