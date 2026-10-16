using FluentValidation;

using Microsoft.AspNetCore.Mvc.Filters;

namespace SBus.Web.Api.Errors;

public sealed class ApiValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ApiProblems.IsApi(context.HttpContext))
        {
            foreach (var value in context.ActionArguments.Values.Where(x => x is not null))
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(value!.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
                {
                    var result = await validator.ValidateAsync(new ValidationContext<object>(value), context.HttpContext.RequestAborted);
                    if (!result.IsValid)
                    {
                        throw new ValidationException(result.Errors);
                    }
                }
            }
        }

        await next();
    }
}