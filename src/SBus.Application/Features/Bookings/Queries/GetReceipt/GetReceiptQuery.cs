using MediatR;

using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetReceipt;

public sealed record GetReceiptQuery(Guid BookingId) : IRequest<Result<ReceiptFileDto>>;
