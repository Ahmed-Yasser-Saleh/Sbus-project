using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;
using SBus.Domain.Bookings;

namespace SBus.Application.Features.Bookings.Queries.GetMyTickets;

public sealed record MyTicketDto(Guid Id, Guid TripId, string PublicToken, BookingStatus Status,
    DateTimeOffset CreatedAtUtc, DateOnly ServiceDate, TimeOnly DepartureTime);
