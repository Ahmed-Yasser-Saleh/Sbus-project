using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Domain.Drivers;
using SBus.Domain.Fleet;
using SBus.Domain.Routes;
using SBus.Domain.Schedules;
using SBus.Domain.Stops;
using SBus.Infrastructure.Identity;
using SBus.Infrastructure.Settings;

namespace SBus.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<AppSettings> options)
{
    public const string DemoLayoutGrid = """
        D _ 1 2
        3 4 _ 5
        6 7 _ 8
        9 10 _ 11
        12 13 14
        """;

    private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly AppSettings _settings = options.Value;

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await SeedOfficeUserAsync();

            if (_settings.SeedDemoData)
            {
                await SeedDemoDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedOfficeUserAsync()
    {
        if (!await _roleManager.RoleExistsAsync(Roles.Office))
        {
            await _roleManager.CreateAsync(new IdentityRole(Roles.Office));
        }

        if (string.IsNullOrWhiteSpace(_settings.OfficeUserEmail) || string.IsNullOrWhiteSpace(_settings.OfficeUserPassword))
        {
            _logger.LogWarning("AppSettings:OfficeUserEmail / OfficeUserPassword are empty. No office user was seeded.");
            return;
        }

        if (await _userManager.FindByEmailAsync(_settings.OfficeUserEmail) is not null)
        {
            return;
        }

        var user = new AppUser
        {
            UserName = _settings.OfficeUserEmail,
            Email = _settings.OfficeUserEmail,
            EmailConfirmed = true,
        };

        var created = await _userManager.CreateAsync(user, _settings.OfficeUserPassword);

        if (!created.Succeeded)
        {
            throw new InvalidOperationException("Office user seeding failed: " + string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, Roles.Office);

        _logger.LogInformation("Seeded office user {Email}", _settings.OfficeUserEmail);
    }

    private async Task SeedDemoDataAsync()
    {
        if (await _context.Stops.AnyAsync())
        {
            return;
        }

        var ramses = Stop.Create(Guid.CreateVersion7(), "رمسيس", "قدام محطة مصر").Value;
        var abbassia = Stop.Create(Guid.CreateVersion7(), "العباسية", "قدام جامعة عين شمس").Value;
        var nasrCity = Stop.Create(Guid.CreateVersion7(), "مدينة نصر - الحي العاشر", null).Value;
        var tagamoa = Stop.Create(Guid.CreateVersion7(), "التجمع الخامس", "أول التسعين الجنوبي").Value;
        var suez = Stop.Create(Guid.CreateVersion7(), "مكتب S Bus - السويس", null).Value;

        _context.Stops.AddRange(ramses, abbassia, nasrCity, tagamoa, suez);

        _context.RouteStops.AddRange(RouteStop.CreateSequence(Direction.CairoToSuez, [
            (ramses.Id, 0),
            (abbassia.Id, 15),
            (nasrCity.Id, 35),
            (tagamoa.Id, 55),
            (suez.Id, 150),
        ]).Value);

        _context.RouteStops.AddRange(RouteStop.CreateSequence(Direction.SuezToCairo, [
            (suez.Id, 0),
            (tagamoa.Id, 95),
            (nasrCity.Id, 115),
            (abbassia.Id, 135),
            (ramses.Id, 150),
        ]).Value);

        var layout = SeatLayout.FromGrid(Guid.CreateVersion7(), "ميني باص 14 راكب", DemoLayoutGrid).Value;
        _context.SeatLayouts.Add(layout);

        var bus1 = Bus.Create(Guid.CreateVersion7(), "س ب ع 1234", layout.Id).Value;
        var bus2 = Bus.Create(Guid.CreateVersion7(), "س ب ع 5678", layout.Id).Value;
        _context.Buses.AddRange(bus1, bus2);

        var driver1 = Driver.Create(Guid.CreateVersion7(), "أحمد محمود (ديمو)", "01000000001").Value;
        var driver2 = Driver.Create(Guid.CreateVersion7(), "محمد علي (ديمو)", "01100000002").Value;
        _context.Drivers.AddRange(driver1, driver2);

        _context.Schedules.AddRange(
            Schedule.Create(Guid.CreateVersion7(), Direction.SuezToCairo, new TimeOnly(6, 0), 150, bus1.Id, driver1.Id).Value,
            Schedule.Create(Guid.CreateVersion7(), Direction.SuezToCairo, new TimeOnly(13, 0), 150, bus2.Id, driver2.Id).Value,
            Schedule.Create(Guid.CreateVersion7(), Direction.SuezToCairo, new TimeOnly(18, 0), 150, bus1.Id, driver1.Id).Value,
            Schedule.Create(Guid.CreateVersion7(), Direction.CairoToSuez, new TimeOnly(9, 0), 150, bus1.Id, driver1.Id).Value,
            Schedule.Create(Guid.CreateVersion7(), Direction.CairoToSuez, new TimeOnly(16, 0), 150, bus2.Id, driver2.Id).Value,
            Schedule.Create(Guid.CreateVersion7(), Direction.CairoToSuez, new TimeOnly(21, 30), 150, bus1.Id, driver1.Id).Value);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded demo stops, route, bus, drivers and schedules.");
    }
}
