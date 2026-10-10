using Microsoft.EntityFrameworkCore;

using SBus.Infrastructure.Data;

namespace SBus.Infrastructure.Identity;

public interface IIdentityTransaction
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<T, bool> commit);
}
