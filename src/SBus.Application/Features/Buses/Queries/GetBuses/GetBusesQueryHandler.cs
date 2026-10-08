using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Buses.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Queries.GetBuses;

public class GetBusesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetBusesQuery, Result<List<BusDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<BusDto>>> Handle(GetBusesQuery query, CancellationToken ct)
    {
        return await _context.Buses
            .AsNoTracking()
            .Where(b => !query.ActiveOnly || b.IsActive)
            .OrderBy(b => b.PlateNumber)
            .Select(b => new BusDto(
                b.Id,
                b.PlateNumber,
                b.SeatLayoutId,
                b.SeatLayout!.Name,
                b.SeatLayout.Seats.Count(),
                b.IsActive))
            .ToListAsync(ct);
    }
}
