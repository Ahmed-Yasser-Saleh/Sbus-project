using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Validation;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Commands.UpdateDriver;

public class UpdateDriverCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateDriverCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Updated>> Handle(UpdateDriverCommand command, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == command.DriverId, ct);

        if (driver is null)
        {
            return ApplicationErrors.DriverNotFound;
        }

        var result = driver.Update(command.Name, EgyptianPhone.Normalize(command.PhoneNumber)!, command.IsActive);

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}
