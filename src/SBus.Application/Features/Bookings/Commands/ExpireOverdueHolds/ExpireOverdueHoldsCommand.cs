using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.ExpireOverdueHolds;

public sealed record ExpireOverdueHoldsCommand : IRequest<Result<int>>;
