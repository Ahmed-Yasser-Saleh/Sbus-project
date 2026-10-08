using MediatR;

using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Validation;
using SBus.Domain.Common.Results;
using SBus.Domain.Drivers;

namespace SBus.Application.Features.Drivers.Commands.CreateDriver;

public class CreateDriverCommandHandler(
    ILogger<CreateDriverCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CreateDriverCommand, Result<Guid>>
{
    private readonly ILogger<CreateDriverCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;

    public async Task<Result<Guid>> Handle(CreateDriverCommand command, CancellationToken ct)
    {
        var result = Driver.Create(Guid.CreateVersion7(), command.Name, EgyptianPhone.Normalize(command.PhoneNumber)!);

        if (result.IsError)
        {
            return result.Errors;
        }

        _context.Drivers.Add(result.Value);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Driver created. Id: {DriverId}", result.Value.Id);

        return result.Value.Id;
    }
}
