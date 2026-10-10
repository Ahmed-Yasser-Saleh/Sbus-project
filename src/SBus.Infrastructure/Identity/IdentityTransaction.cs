using Microsoft.EntityFrameworkCore;

using SBus.Infrastructure.Data;

namespace SBus.Infrastructure.Identity;

public sealed class IdentityTransaction(AppDbContext context) : IIdentityTransaction
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await action();
        if (commit(result))
        {
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
        }

        return result;
    }
}
