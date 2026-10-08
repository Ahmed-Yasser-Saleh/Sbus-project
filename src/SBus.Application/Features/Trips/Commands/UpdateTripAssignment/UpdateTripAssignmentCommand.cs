using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Commands.UpdateTripAssignment;

public sealed record UpdateTripAssignmentCommand(Guid TripId, Guid BusId, Guid DriverId) : IRequest<Result<Updated>>;
