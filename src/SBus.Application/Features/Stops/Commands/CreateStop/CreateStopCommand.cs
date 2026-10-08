using MediatR;

using SBus.Application.Features.Stops.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Stops.Commands.CreateStop;

public sealed record CreateStopCommand(string Name, string? Description) : IRequest<Result<StopDto>>;
