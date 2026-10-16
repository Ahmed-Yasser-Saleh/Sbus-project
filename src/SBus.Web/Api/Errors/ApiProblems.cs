using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using SBus.Application.Common.Errors;

namespace SBus.Web.Api.Errors;

public static class ApiProblems
{
    public const string ContentType = "application/problem+json";
    public static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    public static void Customize(ProblemDetailsContext context)
    {
        if (!IsApi(context.HttpContext))
        {
            return;
        }

        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;
        var (category, title, code) = Defaults(status);
        problem.Status = status;
        problem.Type = $"https://sbus.app/errors/{category}";
        problem.Title = title;
        problem.Detail ??= title;
        problem.Instance = context.HttpContext.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        problem.Extensions.TryAdd("errorCode", code);
    }

    public static Task WriteAsync(HttpContext http, int status, string? code = null, string? detail = null, Exception? exception = null)
    {
        http.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Detail = detail };
        if (code is not null)
        {
            problem.Extensions["errorCode"] = code;
        }

        return http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problem,
            Exception = exception,
        }).AsTask();
    }

    private static (string Category, string Title, string Code) Defaults(int status) => status switch
    {
        400 => ("validation", "One or more validation errors occurred.", ErrorCodes.ValidationFailed),
        401 => ("authentication", "Authentication is required.", ErrorCodes.Unauthenticated),
        403 => ("forbidden", "Access is forbidden.", ErrorCodes.Forbidden),
        404 => ("not-found", "The resource was not found.", ErrorCodes.RouteNotFound),
        405 => ("request", "This method is not allowed.", ErrorCodes.MethodNotAllowed),
        409 => ("conflict", "The request conflicts with the current state.", ErrorCodes.Conflict),
        413 => ("request", "The request is too large.", ErrorCodes.RequestTooLarge),
        415 => ("request", "The content type is not supported.", ErrorCodes.UnsupportedMediaType),
        422 => ("business-rule", "The request violates a business rule.", ErrorCodes.BusinessRule),
        429 => ("rate-limit", "Too many requests. Try again later.", ErrorCodes.RateLimitExceeded),
        503 => ("unavailable", "The service is unavailable.", ErrorCodes.ApiUnavailable),
        _ => ("internal", "An unexpected error occurred.", ErrorCodes.InternalError),
    };
}