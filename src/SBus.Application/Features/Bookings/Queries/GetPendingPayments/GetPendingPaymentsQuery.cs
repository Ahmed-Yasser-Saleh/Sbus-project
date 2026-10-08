using MediatR;

using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetPendingPayments;

public sealed record GetPendingPaymentsQuery : IRequest<Result<List<PendingPaymentDto>>>;
