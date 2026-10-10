using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Bookings;
using SBus.Domain.Common;
using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;
using SBus.Domain.Schedules;
using SBus.Domain.Stops;
using SBus.Domain.Trips;
using SBus.Infrastructure.Identity;

using IdentityRoles = SBus.Infrastructure.Identity.Roles;

namespace SBus.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<SeatLayout> SeatLayouts => Set<SeatLayout>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new UniqueConstraintException(pg.ConstraintName, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AppUser>().HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("EmailIndex").IsUnique();
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "sbus-traveler", Name = IdentityRoles.Traveler, NormalizedName = "TRAVELER", ConcurrencyStamp = "sbus-traveler-v1" },
            new IdentityRole { Id = "sbus-company-owner", Name = IdentityRoles.CompanyOwner, NormalizedName = "COMPANYOWNER", ConcurrencyStamp = "sbus-company-owner-v1" },
            new IdentityRole { Id = "sbus-company-employee", Name = IdentityRoles.CompanyEmployee, NormalizedName = "COMPANYEMPLOYEE", ConcurrencyStamp = "sbus-company-employee-v1" });
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker.Entries()
            .Where(e => e.Entity is Entity baseEntity && baseEntity.DomainEvents.Count != 0)
            .Select(e => (Entity)e.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }
    }
}
