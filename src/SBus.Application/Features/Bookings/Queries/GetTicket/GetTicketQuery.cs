using MediatR;

using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetTicket;

public sealed record GetTicketQuery(string PublicToken) : IRequest<Result<TicketDto>>;
