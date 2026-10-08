using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.ConfirmPayment;

public class ConfirmPaymentCommandHandler(
    ILogger<ConfirmPaymentCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmPaymentCommand, Result<Updated>>
{
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(ConfirmPaymentCommand command, CancellationToken ct)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == command.BookingId, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var result = booking.Confirm(_timeProvider.GetUtcNow());

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Payment confirmed for booking {BookingId}", booking.Id);

        return Result.Updated;
    }
}
