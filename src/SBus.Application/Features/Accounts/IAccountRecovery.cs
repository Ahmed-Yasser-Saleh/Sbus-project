namespace SBus.Application.Features.Accounts;

public interface IAccountRecovery
{
    Task SendConfirmationAsync(string email, CancellationToken ct);
    Task SendResetAsync(string email, CancellationToken ct);
    Task<bool> ConfirmAsync(string userId, string code, CancellationToken ct);
    Task<bool> ResetAsync(string userId, string code, string password, CancellationToken ct);
}