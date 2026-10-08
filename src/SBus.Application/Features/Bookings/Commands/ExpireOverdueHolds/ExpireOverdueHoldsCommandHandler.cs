using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.ExpireOverdueHolds;

public class ExpireOverdueHoldsCommandHandler(
    ILogger<ExpireOverdueHoldsCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<ExpireOverdueHoldsCommand, Result<int>>
{
    private readonly ILogger<ExpireOverdueHoldsCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<int>> Handle(ExpireOverdueHoldsCommand command, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var overdue = await _context.Bookings
            .Include(b => b.Seats)
            .Where(b => b.Status == BookingStatus.HoldPendingPayment && b.HoldExpiresAtUtc <= nowUtc)
            .ToListAsync(ct);

        if (overdue.Count == 0)
        {
            return 0;
        }

        foreach (var booking in overdue)
        {
            booking.Expire(nowUtc);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Expired {Count} unpaid holds", overdue.Count);

        return overdue.Count;
    }
}
