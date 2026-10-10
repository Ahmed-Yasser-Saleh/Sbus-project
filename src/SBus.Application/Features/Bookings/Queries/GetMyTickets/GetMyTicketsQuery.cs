using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Domain.Bookings;

namespace SBus.Application.Features.Bookings.Queries.GetMyTickets;

public sealed record GetMyTicketsQuery(int Page = 1) : IRequest<IReadOnlyList<MyTicketDto>>;
