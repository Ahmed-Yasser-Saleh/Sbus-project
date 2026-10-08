using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetPendingPayments;

public class GetPendingPaymentsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPendingPaymentsQuery, Result<List<PendingPaymentDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<PendingPaymentDto>>> Handle(GetPendingPaymentsQuery query, CancellationToken ct)
    {
        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Seats)
            .Include(b => b.Trip)
            .Where(b => b.Status == BookingStatus.AwaitingConfirmation)
            .OrderBy(b => b.ReceiptSubmittedAtUtc)
            .ToListAsync(ct);

        return bookings
            .Select(b => new PendingPaymentDto(
                b.Id,
                b.TripId,
                b.PassengerName,
                b.PhoneNumber,
                b.SeatNumbers,
                b.Amount,
                b.Trip!.Direction,
                b.Trip.ServiceDate,
                b.Trip.DepartureTime,
                b.ReceiptSubmittedAtUtc))
            .ToList();
    }
}
