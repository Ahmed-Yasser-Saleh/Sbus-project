using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Common;

internal static class ScheduleAssignmentRules
{
    public static async Task<Result<Success>> EnsureBusAndDriverActiveAsync(IAppDbContext context, Guid busId, Guid driverId, CancellationToken ct)
    {
        var bus = await context.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == busId, ct);

        if (bus is null)
        {
            return ApplicationErrors.BusNotFound;
        }

        if (!bus.IsActive)
        {
            return ApplicationErrors.BusInactive;
        }

        var driver = await context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct);

        if (driver is null)
        {
            return ApplicationErrors.DriverNotFound;
        }

        if (!driver.IsActive)
        {
            return ApplicationErrors.DriverInactive;
        }

        return Result.Success;
    }
}
