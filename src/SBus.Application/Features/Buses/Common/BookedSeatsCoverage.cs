using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Common;

internal static class BookedSeatsCoverage
{
    public static async Task<Result<Success>> EnsureLayoutCoversAsync(
        IAppDbContext context,
        Guid seatLayoutId,
        IQueryable<Guid> tripIds,
        CancellationToken ct)
    {
        var layoutSeats = await context.SeatLayouts
            .Where(l => l.Id == seatLayoutId)
            .SelectMany(l => l.Seats.Select(s => s.SeatNumber))
            .ToListAsync(ct);

        var bookedSeats = await context.BookingSeats
            .Where(s => s.IsActive && tripIds.Contains(s.TripId))
            .Select(s => s.SeatNumber)
            .Distinct()
            .ToListAsync(ct);

        var missing = bookedSeats.Except(layoutSeats).Order().ToList();

        if (missing.Count > 0)
        {
            return ApplicationErrors.LayoutDoesNotCoverBookedSeats(missing);
        }

        return Result.Success;
    }
}
