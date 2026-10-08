using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;
using SBus.Domain.Fleet;

namespace SBus.Application.Features.Buses.Commands.CreateBus;

public class CreateBusCommandHandler(
    ILogger<CreateBusCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CreateBusCommand, Result<Guid>>
{
    private readonly ILogger<CreateBusCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<Guid>> Handle(CreateBusCommand command, CancellationToken ct)
    {
        if (!await _context.SeatLayouts.AnyAsync(l => l.Id == command.SeatLayoutId, ct))
        {
            return ApplicationErrors.SeatLayoutNotFound;
        }

        var plate = command.PlateNumber.Trim();

        if (await _context.Buses.AnyAsync(b => b.PlateNumber == plate, ct))
        {
            return ApplicationErrors.PlateNumberExists;
        }

        var result = Bus.Create(Guid.CreateVersion7(), plate, command.SeatLayoutId);

        if (result.IsError)
        {
            return result.Errors;
        }

        _context.Buses.Add(result.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bus created. Id: {BusId}", result.Value.Id);

        return result.Value.Id;
    }
}
