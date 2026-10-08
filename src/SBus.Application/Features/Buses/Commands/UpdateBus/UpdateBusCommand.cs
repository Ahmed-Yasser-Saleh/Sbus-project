using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Buses.Commands.UpdateBus;

public sealed record UpdateBusCommand(Guid BusId, string PlateNumber, Guid SeatLayoutId, bool IsActive) : IRequest<Result<Updated>>;
