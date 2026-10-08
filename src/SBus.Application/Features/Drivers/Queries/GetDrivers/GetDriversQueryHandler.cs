using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Queries.GetDrivers;

public class GetDriversQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDriversQuery, Result<List<DriverDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<DriverDto>>> Handle(GetDriversQuery query, CancellationToken ct)
    {
        return await _context.Drivers
            .AsNoTracking()
            .Where(d => !query.ActiveOnly || d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DriverDto(d.Id, d.Name, d.PhoneNumber, d.IsActive))
            .ToListAsync(ct);
    }
}
