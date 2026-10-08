namespace SBus.Application.Common.Settings;

public class BookingOptions
{
    public const string SectionName = "Booking";

    public int HoldMinutes { get; set; } = 15;
    public int PassengerCancellationCutoffHours { get; set; } = 2;
    public int BookingWindowDays { get; set; } = 7;
    public long MaxReceiptBytes { get; set; } = 5 * 1024 * 1024;
    public string InstaPayAddress { get; set; } = string.Empty;
    public string OfficePhone { get; set; } = string.Empty;

    public TimeSpan HoldDuration => TimeSpan.FromMinutes(HoldMinutes);
    public TimeSpan PassengerCancellationCutoff => TimeSpan.FromHours(PassengerCancellationCutoffHours);
}
