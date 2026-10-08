using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Commands.CreateBus;

public sealed record CreateBusCommand(string PlateNumber, Guid SeatLayoutId) : IRequest<Result<Guid>>;
