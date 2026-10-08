using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.RejectPayment;

public class RejectPaymentCommandHandler(
    ILogger<RejectPaymentCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<RejectPaymentCommand, Result<Updated>>
{
    private readonly ILogger<RejectPaymentCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(RejectPaymentCommand command, CancellationToken ct)
    {
        var booking = await _context.Bookings
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == command.BookingId, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var result = booking.Reject(command.Reason, _timeProvider.GetUtcNow());

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Payment rejected for booking {BookingId}", booking.Id);

        return Result.Updated;
    }
}
