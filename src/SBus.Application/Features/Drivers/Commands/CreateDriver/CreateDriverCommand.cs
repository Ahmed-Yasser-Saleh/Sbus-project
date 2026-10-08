using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Commands.CreateDriver;

public sealed record CreateDriverCommand(string Name, string PhoneNumber) : IRequest<Result<Guid>>;
