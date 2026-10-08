using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Mappers;
using SBus.Domain.Common.Results;
using SBus.Domain.Fleet;

namespace SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;

public class CreateSeatLayoutCommandHandler(
    ILogger<CreateSeatLayoutCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CreateSeatLayoutCommand, Result<SeatLayoutDto>>
{
    private readonly ILogger<CreateSeatLayoutCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<SeatLayoutDto>> Handle(CreateSeatLayoutCommand command, CancellationToken ct)
    {
        var name = command.Name.Trim();

        if (await _context.SeatLayouts.AnyAsync(l => l.Name == name, ct))
        {
            return ApplicationErrors.SeatLayoutNameExists;
        }

        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), name, command.Grid);

        if (result.IsError)
        {
            return result.Errors;
        }

        _context.SeatLayouts.Add(result.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Seat layout created. Id: {SeatLayoutId}, Capacity: {Capacity}", result.Value.Id, result.Value.Capacity);

        return result.Value.ToDto();
    }
}
