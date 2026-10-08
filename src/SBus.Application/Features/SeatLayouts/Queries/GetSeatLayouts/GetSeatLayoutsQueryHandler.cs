using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Mappers;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;

public class GetSeatLayoutsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSeatLayoutsQuery, Result<List<SeatLayoutDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<SeatLayoutDto>>> Handle(GetSeatLayoutsQuery query, CancellationToken ct)
    {
        var layouts = await _context.SeatLayouts
            .AsNoTracking()
            .Include(l => l.Seats)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

        return layouts.ToDtos();
    }
}
