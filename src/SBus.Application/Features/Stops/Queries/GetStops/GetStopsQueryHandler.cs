using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Mappers;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Stops.Queries.GetStops;

public class GetStopsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetStopsQuery, Result<List<StopDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<StopDto>>> Handle(GetStopsQuery query, CancellationToken ct)
    {
        var stops = await _context.Stops
            .AsNoTracking()
            .Where(s => !query.ActiveOnly || s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return stops.ToDtos();
    }
}
