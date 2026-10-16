namespace SBus.Application.Features.MobileAuth;

public sealed class MobileAuthOptions
{
    public const string SectionName = "Authentication:Mobile";
    public bool Enabled { get; set; }
    public string Issuer { get; set; } = "https://sbus.app";
    public string Audience { get; set; } = "sbus-mobile";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 30;
    public string[] GoogleClientIds { get; set; } = [];
    public string[] CorsOrigins { get; set; } = [];
    public int AuthPermitLimit { get; set; } = 20;
    public int AuthWindowSeconds { get; set; } = 600;
}