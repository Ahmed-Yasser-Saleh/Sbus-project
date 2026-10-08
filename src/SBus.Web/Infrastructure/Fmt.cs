using System.Globalization;

using SBus.Application.Common.Time;
using SBus.Domain.Bookings;
using SBus.Domain.Routes;

namespace SBus.Web.Infrastructure;

public static class Fmt
{
    private static readonly CultureInfo Arabic = CultureInfo.GetCultureInfo("ar-EG");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    private static CultureInfo Display => Languages.IsEnglish ? English : Arabic;

    public static string Date(DateOnly date) => date.ToString("dddd d MMMM", Display);

    public static string ShortDate(DateOnly date) => date.ToString("ddd d/M", Display);

    public static string Time(TimeOnly time) => time.ToString("h:mm tt", Display);

    public static string LocalDateTime(DateTimeOffset utc) => CairoTime.ToLocal(utc).ToString("ddd d/M h:mm tt", Display);

    public static string Money(decimal amount)
    {
        var value = amount.ToString("0.##", CultureInfo.InvariantCulture);
        return Languages.IsEnglish ? $"EGP {value}" : $"{value} جنيه";
    }

    public static string Currency => Languages.IsEnglish ? "EGP" : "جنيه";

    public static string Seats(IEnumerable<int> seats) => string.Join(Languages.IsEnglish ? ", " : "، ", seats);

    public static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Direction(Direction direction) => (direction, Languages.IsEnglish) switch
    {
        (Domain.Routes.Direction.CairoToSuez, true) => "Cairo → Suez",
        (Domain.Routes.Direction.SuezToCairo, true) => "Suez → Cairo",
        (Domain.Routes.Direction.CairoToSuez, false) => "القاهرة ← السويس",
        (Domain.Routes.Direction.SuezToCairo, false) => "السويس ← القاهرة",
        _ => direction.ToString(),
    };

    public static string Status(BookingStatus status) => (status, Languages.IsEnglish) switch
    {
        (BookingStatus.HoldPendingPayment, true) => "Waiting for payment",
        (BookingStatus.AwaitingConfirmation, true) => "Office is checking the transfer",
        (BookingStatus.Confirmed, true) => "Confirmed",
        (BookingStatus.Expired, true) => "Expired",
        (BookingStatus.Rejected, true) => "Payment rejected",
        (BookingStatus.Cancelled, true) => "Cancelled",
        (BookingStatus.HoldPendingPayment, false) => "مستني التحويل",
        (BookingStatus.AwaitingConfirmation, false) => "المكتب بيراجع التحويل",
        (BookingStatus.Confirmed, false) => "متأكد",
        (BookingStatus.Expired, false) => "المدة خلصت",
        (BookingStatus.Rejected, false) => "التحويل اترفض",
        (BookingStatus.Cancelled, false) => "اتلغى",
        _ => status.ToString(),
    };

    public static string StatusCss(BookingStatus status) => status switch
    {
        BookingStatus.HoldPendingPayment => "text-bg-warning",
        BookingStatus.AwaitingConfirmation => "text-bg-info",
        BookingStatus.Confirmed => "text-bg-success",
        BookingStatus.Rejected => "text-bg-danger",
        _ => "text-bg-secondary",
    };

    public static string Source(BookingSource source) => (source, Languages.IsEnglish) switch
    {
        (BookingSource.Online, true) => "Online",
        (BookingSource.Office, true) => "Office",
        (BookingSource.Online, false) => "أونلاين",
        (BookingSource.Office, false) => "المكتب",
        _ => source.ToString(),
    };
}
