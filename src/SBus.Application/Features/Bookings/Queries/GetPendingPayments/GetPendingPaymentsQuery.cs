using MediatR;

using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetPendingPayments;

public sealed record GetPendingPaymentsQuery(SBus.Application.Common.Models.QueryPage? Page = null) : IRequest<Result<List<PendingPaymentDto>>>;
