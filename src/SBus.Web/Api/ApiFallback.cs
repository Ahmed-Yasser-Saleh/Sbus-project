using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing.Template;

using Microsoft.Net.Http.Headers;

using SBus.Web.Api.Errors;

namespace SBus.Web.Api;

public static class ApiFallback
{
    public static Task HandleAsync(HttpContext http)
    {
        var endpoints = http.RequestServices.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        var candidates = endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null
                && endpoint.RoutePattern.RawText is { } pattern
                && new TemplateMatcher(TemplateParser.Parse(pattern), new RouteValueDictionary()).TryMatch(http.Request.Path, new RouteValueDictionary()))
            .ToArray();
        var methods = candidates.SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).Distinct().ToArray();
        var contentTypes = candidates.Where(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(http.Request.Method, StringComparer.OrdinalIgnoreCase) == true)
            .SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<ConsumesAttribute>())
            .SelectMany(attribute => attribute.ContentTypes).ToArray();
        if (contentTypes.Length > 0 && (!MediaTypeHeaderValue.TryParse(http.Request.ContentType, out var incoming)
            || !contentTypes.Any(accepted => incoming.IsSubsetOf(MediaTypeHeaderValue.Parse(accepted)))))
        {
            return ApiProblems.WriteAsync(http, 415);
        }

        if (methods.Length == 0 || methods.Contains(http.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return ApiProblems.WriteAsync(http, 404);
        }

        http.Response.Headers.Allow = string.Join(", ", methods);
        return ApiProblems.WriteAsync(http, 405);
    }
}
