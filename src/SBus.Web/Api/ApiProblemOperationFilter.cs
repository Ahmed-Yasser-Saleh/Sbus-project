using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

using SBus.Web.Api.Errors;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace SBus.Web.Api;

public sealed class ApiProblemOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
        {
            operation.Security = [];
        }

        if (operation.Responses is null)
        {
            return;
        }

        foreach (var (status, response) in operation.Responses)
        {
            if (int.TryParse(status, out var code) && code >= 400 && response is OpenApiResponse mutable)
            {
                var schema = mutable.Content?.Values.FirstOrDefault()?.Schema;
                mutable.Content = new Dictionary<string, OpenApiMediaType>
                {
                    [ApiProblems.ContentType] = new() { Schema = schema },
                };
            }
        }
    }
}