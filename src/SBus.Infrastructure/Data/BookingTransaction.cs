using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Interfaces;

namespace SBus.Infrastructure.Data;

public sealed class BookingTransaction(AppDbContext context) : IBookingTransaction
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var committed = false;
        try
        {
            var result = await action();
            if (commit(result))
            {
                await transaction.CommitAsync(ct);
                committed = true;
            }

            return result;
        }
        finally
        {
            if (!committed)
            {
                // Disposal rolls back even when cancellation or an exception interrupts the action.
                context.ChangeTracker.Clear();
            }
        }
    }
}
