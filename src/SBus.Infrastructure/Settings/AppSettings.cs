namespace SBus.Infrastructure.Settings;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public int HoldExpiryCheckSeconds { get; set; } = 60;
    public int TripGenerationIntervalMinutes { get; set; } = 60;
    public string ReceiptsPath { get; set; } = "App_Data/receipts";
    public bool SeedDemoData { get; set; }
    public string? OfficeUserEmail { get; set; }
    public string? OfficeUserPassword { get; set; }
}
