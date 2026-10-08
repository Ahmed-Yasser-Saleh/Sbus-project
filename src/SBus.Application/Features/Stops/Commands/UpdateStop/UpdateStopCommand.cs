using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Stops.Commands.UpdateStop;

public sealed record UpdateStopCommand(Guid StopId, string Name, string? Description, bool IsActive) : IRequest<Result<Updated>>;
