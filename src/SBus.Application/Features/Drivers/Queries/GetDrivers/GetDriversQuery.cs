using MediatR;

using SBus.Application.Features.Drivers.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Queries.GetDrivers;

public sealed record GetDriversQuery(bool ActiveOnly = false) : IRequest<Result<List<DriverDto>>>;
