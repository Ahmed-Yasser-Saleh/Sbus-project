namespace SBus.Application.Common.Interfaces;

public interface IRegistrationConfirmationSender
{
    Task SendAsync(string userId, CancellationToken ct);
}

