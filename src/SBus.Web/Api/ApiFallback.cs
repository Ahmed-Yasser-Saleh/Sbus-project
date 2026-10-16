using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing.Template;

using SBus.Web.Api.Errors;

namespace SBus.Web.Api;

public static class ApiFallback
{
    public static Task HandleAsync(HttpContext http)
    {
        var endpoints = http.RequestServices.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        var methods = endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null
                && endpoint.RoutePattern.RawText is { } pattern
                && new TemplateMatcher(TemplateParser.Parse(pattern), new RouteValueDictionary()).TryMatch(http.Request.Path, new RouteValueDictionary()))
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).Distinct().ToArray();
        if (methods.Length == 0 || methods.Contains(http.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return ApiProblems.WriteAsync(http, 404);
        }

        http.Response.Headers.Allow = string.Join(", ", methods);
        return ApiProblems.WriteAsync(http, 405);
    }
}