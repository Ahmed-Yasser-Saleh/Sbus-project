using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SBus.Web.Api.Errors;

public sealed class ApiJsonProblemWriter(IOptions<ProblemDetailsOptions> options, ILogger<ApiJsonProblemWriter> logger) : IProblemDetailsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public bool CanWrite(ProblemDetailsContext context) => ApiProblems.IsApi(context.HttpContext);

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        options.Value.CustomizeProblemDetails?.Invoke(context);
        var problem = context.ProblemDetails;
        if (problem.Status >= 500)
        {
            logger.LogError(context.Exception, "API failure {Status} {ErrorCode} TraceId {TraceId}", problem.Status,
                problem.Extensions["errorCode"], problem.Extensions["traceId"]);
        }
        else
        {
            logger.LogInformation("API rejection {Status} {ErrorCode} TraceId {TraceId}", problem.Status,
                problem.Extensions["errorCode"], problem.Extensions["traceId"]);
        }

        context.HttpContext.Response.StatusCode = problem.Status ?? 500;
        context.HttpContext.Response.ContentType = ApiProblems.ContentType;
        await JsonSerializer.SerializeAsync(context.HttpContext.Response.Body, problem, problem.GetType(), JsonOptions, context.HttpContext.RequestAborted);
    }
}