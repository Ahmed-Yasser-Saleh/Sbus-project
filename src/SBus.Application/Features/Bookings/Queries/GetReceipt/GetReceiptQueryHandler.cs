using MediatR;

using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Queries.GetReceipt;

public class GetReceiptQueryHandler(IAppDbContext context, IReceiptStorage storage)
    : IRequestHandler<GetReceiptQuery, Result<ReceiptFileDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly IReceiptStorage _storage = storage;

    public async Task<Result<ReceiptFileDto>> Handle(GetReceiptQuery query, CancellationToken ct)
    {
        var fileName = await _context.Bookings
            .Where(b => b.Id == query.BookingId)
            .Select(b => b.ReceiptFileName)
            .FirstOrDefaultAsync(ct);

        if (fileName is null)
        {
            return ApplicationErrors.ReceiptNotFound;
        }

        var stream = _storage.OpenRead(fileName);

        if (stream is null)
        {
            return ApplicationErrors.ReceiptNotFound;
        }

        return new ReceiptFileDto(stream, ReceiptFileType.ContentTypeFor(fileName), fileName);
    }
}
