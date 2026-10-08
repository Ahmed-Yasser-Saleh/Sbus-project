using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CancelBookingByOffice;

public class CancelBookingByOfficeCommandHandler(
    ILogger<CancelBookingByOfficeCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider)
    : IRequestHandler<CancelBookingByOfficeCommand, Result<Updated>>
{
    private readonly ILogger<CancelBookingByOfficeCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(CancelBookingByOfficeCommand command, CancellationToken ct)
    {
        var booking = await _context.Bookings
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == command.BookingId, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var result = booking.CancelByOffice(_timeProvider.GetUtcNow());

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {BookingId} cancelled by office", booking.Id);

        return Result.Updated;
    }
}
