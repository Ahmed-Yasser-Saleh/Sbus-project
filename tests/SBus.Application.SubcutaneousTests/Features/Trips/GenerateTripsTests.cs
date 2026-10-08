using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SBus.Application.Features.Trips.Commands.GenerateTrips;
using SBus.Application.SubcutaneousTests.Common;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Trips;

[Collection(WebAppFactoryCollection.Name)]
public class GenerateTripsTests(WebAppFactory factory)
{
    private readonly WebAppFactory _factory = factory;

    [DatabaseFact]
    public async Task RunningTwice_DoesNotDuplicateTrips()
    {
        using var scope = _factory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new GenerateTripsCommand(7));
        var second = await sender.Send(new GenerateTripsCommand(7));

        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value);

        await using var db = _factory.CreateDbContext();
        var perDate = await db.Trips
            .Where(t => t.ScheduleId == _factory.World.ScheduleId)
            .GroupBy(t => t.ServiceDate)
            .Select(g => g.Count())
            .ToListAsync();

        Assert.All(perDate, count => Assert.Equal(1, count));
    }
}
