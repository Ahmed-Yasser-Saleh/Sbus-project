using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CancelBookingByOffice;

public sealed record CancelBookingByOfficeCommand(Guid BookingId) : IRequest<Result<Updated>>;
