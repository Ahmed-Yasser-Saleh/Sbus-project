using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Routes.Dtos;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;

namespace SBus.Application.Features.Routes.Queries.GetRoute;

public class GetRouteQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRouteQuery, Result<List<RouteStopDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<RouteStopDto>>> Handle(GetRouteQuery query, CancellationToken ct)
    {
        return await LoadAsync(_context, query.Direction, ct);
    }

    internal static Task<List<RouteStopDto>> LoadAsync(IAppDbContext context, Direction direction, CancellationToken ct)
    {
        return context.RouteStops
            .AsNoTracking()
            .Where(r => r.Direction == direction)
            .OrderBy(r => r.Order)
            .Select(r => new RouteStopDto(r.StopId, r.Stop!.Name, r.Order, r.MinutesFromStart))
            .ToListAsync(ct);
    }
}
