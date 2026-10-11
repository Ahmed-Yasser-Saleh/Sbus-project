using Microsoft.AspNetCore.Identity;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Domain.Common.Results;

namespace SBus.Infrastructure.Identity;

public sealed class TravelerProfiles(UserManager<AppUser> users, IUser currentUser) : ITravelerProfiles
{
    public async Task<TravelerProfileDto?> GetCurrentAsync(CancellationToken ct)
    {
        var user = await FindCurrentAsync(ct);
        return user is null ? null : new(user.FullName, user.PhoneNumber);
    }

    public async Task<Result<Success>> UpdateCurrentAsync(string fullName, string phoneNumber, CancellationToken ct)
    {
        var user = await FindCurrentAsync(ct);
        if (user is null)
        {
            return Error.Validation(string.Empty, "تعذّر حفظ بياناتك. جرّب تاني.");
        }

        user.FullName = fullName;
        if (user.PhoneNumber != phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = false;
        }

        var result = await users.UpdateAsync(user);
        return result.Succeeded ? Result.Success : Error.Validation(string.Empty, "تعذّر حفظ بياناتك. جرّب تاني.");
    }

    private async Task<AppUser?> FindCurrentAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!currentUser.IsTraveler || currentUser.Id is null)
        {
            return null;
        }

        return await users.FindByIdAsync(currentUser.Id);
    }
}
