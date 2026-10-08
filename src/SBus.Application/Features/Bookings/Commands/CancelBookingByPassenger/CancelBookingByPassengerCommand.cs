using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CancelBookingByPassenger;

public sealed record CancelBookingByPassengerCommand(string PublicToken) : IRequest<Result<Updated>>;
