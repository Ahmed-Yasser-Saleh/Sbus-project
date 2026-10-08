using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.ConfirmPayment;

public sealed record ConfirmPaymentCommand(Guid BookingId) : IRequest<Result<Updated>>;
