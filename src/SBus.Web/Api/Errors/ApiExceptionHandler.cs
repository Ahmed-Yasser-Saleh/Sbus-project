using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;

namespace SBus.Web.Api.Errors;

public sealed class ApiExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (!ApiProblems.IsApi(http))
        {
            return false;
        }

        if (exception is RequestValidationException request)
        {
            http.Response.StatusCode = 400;
            var problem = new ValidationProblemDetails(request.Errors.ToDictionary(x => x.Key, x => x.Value)) { Status = 400 };
            problem.Extensions["errorCode"] = request.ErrorCode;
            await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                Exception = exception,
                ProblemDetails = problem,
            });
            return true;
        }

        if (exception is ValidationException validation)
        {
            var errors = validation.Errors.GroupBy(x => JsonField(x.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).Distinct().ToArray());
            http.Response.StatusCode = 400;
            await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                Exception = exception,
                ProblemDetails = new ValidationProblemDetails(errors) { Status = 400 },
            });
            return true;
        }

        var status = exception switch
        {
            NotFoundException => 404,
            BusinessRuleException => 422,
            ConflictException => 409,
            UnauthenticatedException => 401,
            ForbiddenException => 403,
            ServiceUnavailableException => 503,
            DbUpdateConcurrencyException or UniqueConstraintException => 409,
            BadHttpRequestException bad => bad.StatusCode,
            _ => 500,
        };
        var code = exception is ApiException api ? api.ErrorCode
            : exception is DbUpdateConcurrencyException or UniqueConstraintException ? ErrorCodes.ConcurrencyConflict : null;
        var detail = status >= 500 && !environment.IsDevelopment()
            ? "The request could not be completed. Try again later."
            : exception is ApiException || environment.IsDevelopment() ? exception.Message : null;
        await ApiProblems.WriteAsync(http, status, code, detail, exception);
        return true;
    }

    public static string JsonField(string path) => string.Join('.', path.Split('.').Select(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName));
}