using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Domain.Bookings;

namespace SBus.Application.Features.Bookings.Queries.GetMyTickets;

public sealed class GetMyTicketsQueryHandler(IAppDbContext context, IUser user)
    : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<MyTicketDto>>
{
    public async Task<IReadOnlyList<MyTicketDto>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        if (user.Id is null || !user.IsTraveler)
        {
            return [];
        }

        var page = Math.Clamp(request.Page, 1, 10000);
        return await context.Bookings.AsNoTracking()
            .Where(b => b.UserId == user.Id)
            .OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.Id)
            .Skip((page - 1) * 20).Take(20)
            .Select(b => new MyTicketDto(b.Id, b.TripId, b.PublicToken, b.Status,
                b.CreatedAtUtc, b.Trip!.ServiceDate, b.Trip.DepartureTime))
            .ToListAsync(ct);
    }
}
