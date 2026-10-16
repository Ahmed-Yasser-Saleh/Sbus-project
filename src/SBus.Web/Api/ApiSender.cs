using FluentValidation;

using MediatR;

using SBus.Domain.Common.Results;
using SBus.Web.Api.Errors;

namespace SBus.Web.Api;

/// <summary>Validates mapped commands with the same validators as Razor/MediatR.</summary>
public sealed class ApiSender(ISender sender, IServiceProvider services, ApiResultAdapter results)
{
    public async Task<T> Send<T>(IRequest<Result<T>> request, CancellationToken ct)
    {
        if (services.GetService(typeof(IValidator<>).MakeGenericType(request.GetType())) is IValidator validator)
        {
            var validation = await validator.ValidateAsync(new ValidationContext<object>(request), ct);
            if (!validation.IsValid)
            {
                throw new ValidationException(validation.Errors);
            }
        }

        return results.Value(await sender.Send(request, ct));
    }
}