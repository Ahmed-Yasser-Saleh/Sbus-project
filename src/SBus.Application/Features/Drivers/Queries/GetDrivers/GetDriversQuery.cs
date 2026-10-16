using MediatR;

using SBus.Application.Features.Drivers.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Drivers.Queries.GetDrivers;

public sealed record GetDriversQuery(bool ActiveOnly = false, SBus.Application.Common.Models.QueryPage? Page = null) : IRequest<Result<List<DriverDto>>>;
