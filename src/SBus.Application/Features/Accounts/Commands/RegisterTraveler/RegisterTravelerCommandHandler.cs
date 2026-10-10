using MediatR;

using SBus.Application.Common.Interfaces;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Accounts.Commands.RegisterTraveler;

public sealed class RegisterTravelerCommandHandler(
    ITravelerRegistration registration,
    IRegistrationConfirmationSender confirmationSender)
    : IRequestHandler<RegisterTravelerCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(RegisterTravelerCommand request, CancellationToken ct)
    {
        var result = await registration.RegisterAsync(request.Email!, request.Password!, ct);
        if (result.IsError)
        {
            return result.Errors;
        }

        if (result.Value.UserId is not { } userId)
        {
            return Error.Validation(
                nameof(RegisterTravelerCommand.Email),
                "تعذّر إنشاء الحساب بهذه البيانات. جرّب تسجيل الدخول أو استعادة كلمة المرور.");
        }

        await confirmationSender.SendAsync(userId, ct);
        return Result.Success;
    }
}

