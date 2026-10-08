using MediatR;

using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.SubmitReceipt;

public sealed record SubmitReceiptCommand(string PublicToken, Stream Content, long Length) : IRequest<Result<Updated>>;
