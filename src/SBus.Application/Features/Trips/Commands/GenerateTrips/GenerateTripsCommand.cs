using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Commands.GenerateTrips;

public sealed record GenerateTripsCommand(int DaysAhead) : IRequest<Result<int>>;
