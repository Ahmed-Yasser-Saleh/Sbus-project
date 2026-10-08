using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Mappers;
using SBus.Domain.Common.Results;
using SBus.Domain.Stops;

namespace SBus.Application.Features.Stops.Commands.CreateStop;

public class CreateStopCommandHandler(
    ILogger<CreateStopCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CreateStopCommand, Result<StopDto>>
{
    private readonly ILogger<CreateStopCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<StopDto>> Handle(CreateStopCommand command, CancellationToken ct)
    {
        var name = command.Name.Trim();

        if (await _context.Stops.AnyAsync(s => s.Name == name, ct))
        {
            return ApplicationErrors.StopNameExists;
        }

        var result = Stop.Create(Guid.CreateVersion7(), name, command.Description);

        if (result.IsError)
        {
            return result.Errors;
        }

        _context.Stops.Add(result.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Stop created. Id: {StopId}", result.Value.Id);

        return result.Value.ToDto();
    }
}
