using Microsoft.AspNetCore.Mvc.Filters;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Interfaces;

namespace SBus.Web.Api;

public sealed class CompleteApiProfileFilter(ITravelerProfiles profiles) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if ((await profiles.GetCurrentAsync(context.HttpContext.RequestAborted))?.IsComplete != true)
        {
            throw new BusinessRuleException("Complete your name and mobile number before continuing.", ErrorCodes.ProfileIncomplete);
        }

        await next();
    }
}