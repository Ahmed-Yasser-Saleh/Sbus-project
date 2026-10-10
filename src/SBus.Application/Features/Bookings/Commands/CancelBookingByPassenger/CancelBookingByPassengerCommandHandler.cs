using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Common.Settings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;

public class CancelBookingByPassengerCommandHandler(
    ILogger<CancelBookingByPassengerCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options,
    IUser user)
    : IRequestHandler<CancelBookingByPassengerCommand, Result<Updated>>
{
    private readonly ILogger<CancelBookingByPassengerCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<Updated>> Handle(CancelBookingByPassengerCommand command, CancellationToken ct)
    {
        var booking = await _context.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Trip)
            .Where(BookingOwnership.AccessibleTo(user.IsTraveler ? user.Id : null))
            .FirstOrDefaultAsync(b => b.PublicToken == command.PublicToken, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var result = booking.CancelByPassenger(_timeProvider.GetUtcNow(), booking.Trip!.DepartureAtUtc, _options.PassengerCancellationCutoff);

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {BookingId} cancelled by passenger", booking.Id);

        return Result.Updated;
    }
}
