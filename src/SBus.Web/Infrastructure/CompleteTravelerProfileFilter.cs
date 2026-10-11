using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using SBus.Application.Common.Interfaces;

namespace SBus.Web.Infrastructure;

public sealed class CompleteTravelerProfileFilter(IUser user, ITravelerProfiles profiles) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (user.IsTraveler && (await profiles.GetCurrentAsync(context.HttpContext.RequestAborted))?.IsComplete != true)
        {
            var request = context.HttpContext.Request;
            context.Result = new RedirectToPageResult("/Account/CompleteProfile", new
            {
                returnUrl = request.PathBase + request.Path + request.QueryString,
            });
            return;
        }

        await next();
    }
}
