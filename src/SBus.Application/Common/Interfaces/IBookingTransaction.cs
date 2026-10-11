namespace SBus.Application.Common.Interfaces;

public interface IBookingTransaction
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit, CancellationToken ct);
}
