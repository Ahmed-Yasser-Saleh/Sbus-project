using MediatR;

using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetTripForBooking;

public sealed record GetTripForBookingQuery(Guid TripId) : IRequest<Result<TripBookingDto>>;
