using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Stops.Commands.UpdateStop;

public class UpdateStopCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateStopCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Updated>> Handle(UpdateStopCommand command, CancellationToken ct)
    {
        var stop = await _context.Stops.FirstOrDefaultAsync(s => s.Id == command.StopId, ct);

        if (stop is null)
        {
            return ApplicationErrors.StopNotFound;
        }

        var name = command.Name.Trim();

        if (await _context.Stops.AnyAsync(s => s.Name == name && s.Id != command.StopId, ct))
        {
            return ApplicationErrors.StopNameExists;
        }

        var result = stop.Update(name, command.Description, command.IsActive);

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}
