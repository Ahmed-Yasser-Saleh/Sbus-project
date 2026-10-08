using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Schedules.Queries.GetSchedules;

public class GetSchedulesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSchedulesQuery, Result<List<ScheduleDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<ScheduleDto>>> Handle(GetSchedulesQuery query, CancellationToken ct)
    {
        return await _context.Schedules
            .AsNoTracking()
            .OrderBy(s => s.Direction)
            .ThenBy(s => s.DepartureTime)
            .Select(s => new ScheduleDto(
                s.Id,
                s.Direction,
                s.DepartureTime,
                s.Price,
                s.BusId,
                s.Bus!.PlateNumber,
                s.DriverId,
                s.Driver!.Name,
                s.IsActive))
            .ToListAsync(ct);
    }
}
