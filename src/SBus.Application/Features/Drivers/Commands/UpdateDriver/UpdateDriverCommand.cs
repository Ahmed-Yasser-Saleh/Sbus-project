using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Commands.UpdateDriver;

public sealed record UpdateDriverCommand(Guid DriverId, string Name, string PhoneNumber, bool IsActive) : IRequest<Result<Updated>>;
