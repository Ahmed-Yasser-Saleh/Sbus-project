using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Settings;
using SBus.Application.Features.Bookings.Common;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.SubmitReceipt;

public class SubmitReceiptCommandHandler(
    ILogger<SubmitReceiptCommandHandler> logger,
    IAppDbContext context,
    IReceiptStorage storage,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options)
    : IRequestHandler<SubmitReceiptCommand, Result<Updated>>
{
    private readonly ILogger<SubmitReceiptCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly IReceiptStorage _storage = storage;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<Updated>> Handle(SubmitReceiptCommand command, CancellationToken ct)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.PublicToken == command.PublicToken, ct);

        if (booking is null)
        {
            return ApplicationErrors.BookingNotFound;
        }

        var allowed = booking.EnsureCanSubmitReceipt();

        if (allowed.IsError)
        {
            return allowed.Errors;
        }

        using var buffer = new MemoryStream();

        if (!await CopyWithLimitAsync(command.Content, buffer, _options.MaxReceiptBytes, ct))
        {
            return Error.Validation("SubmitReceipt.TooLarge", "حجم الملف أكبر من المسموح.");
        }

        var extension = ReceiptFileType.DetectExtension(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));

        if (extension is null)
        {
            return Error.Validation("SubmitReceipt.UnsupportedFile", "الملف لازم يكون صورة (JPG أو PNG أو WEBP) أو PDF.");
        }

        buffer.Position = 0;
        var fileName = await _storage.SaveAsync(buffer, extension, ct);

        var result = booking.SubmitReceipt(fileName, _timeProvider.GetUtcNow());

        if (result.IsError)
        {
            return result.Errors;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Receipt submitted for booking {BookingId}", booking.Id);

        return Result.Updated;
    }

    private static async Task<bool> CopyWithLimitAsync(Stream source, Stream destination, long maxBytes, CancellationToken ct)
    {
        var chunk = new byte[81920];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(chunk, ct)) > 0)
        {
            total += read;

            if (total > maxBytes)
            {
                return false;
            }

            await destination.WriteAsync(chunk.AsMemory(0, read), ct);
        }

        return true;
    }
}
