using MediatR;

using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;

public sealed record CreateSeatLayoutCommand(string Name, string Grid) : IRequest<Result<SeatLayoutDto>>;
