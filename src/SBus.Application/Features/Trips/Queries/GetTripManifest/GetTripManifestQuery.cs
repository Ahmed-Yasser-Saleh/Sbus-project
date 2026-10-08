using MediatR;

using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Trips.Queries.GetTripManifest;

public sealed record GetTripManifestQuery(Guid TripId) : IRequest<Result<TripManifestDto>>;
