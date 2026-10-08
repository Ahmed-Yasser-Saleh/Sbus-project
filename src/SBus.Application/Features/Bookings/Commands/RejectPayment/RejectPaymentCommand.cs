using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.RejectPayment;

public sealed record RejectPaymentCommand(Guid BookingId, string Reason) : IRequest<Result<Updated>>;
