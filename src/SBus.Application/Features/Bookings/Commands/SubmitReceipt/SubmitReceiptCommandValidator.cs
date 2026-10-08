using FluentValidation;

using Microsoft.Extensions.Options;

using SBus.Application.Common.Settings;

namespace SBus.Application.Features.Bookings.Commands.SubmitReceipt;

public sealed class SubmitReceiptCommandValidator : AbstractValidator<SubmitReceiptCommand>
{
    public SubmitReceiptCommandValidator(IOptions<BookingOptions> options)
    {
        var maxBytes = options.Value.MaxReceiptBytes;

        RuleFor(x => x.PublicToken).NotEmpty();

        RuleFor(x => x.Content).NotNull().WithMessage("ارفع صورة التحويل.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("ارفع صورة التحويل.")
            .LessThanOrEqualTo(maxBytes)
            .WithMessage("حجم الملف لازم يكون أقل من {0} ميجا.")
            .WithState(_ => new object[] { maxBytes / (1024 * 1024) });
    }
}
