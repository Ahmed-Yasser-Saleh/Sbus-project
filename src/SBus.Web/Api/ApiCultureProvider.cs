using Microsoft.AspNetCore.Localization;

using SBus.Web.Api.Errors;
using SBus.Web.Infrastructure;

namespace SBus.Web.Api;

public sealed class ApiCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext http)
    {
        if (!ApiProblems.IsApi(http))
        {
            return Task.FromResult<ProviderCultureResult?>(null);
        }

        var language = http.Request.GetTypedHeaders().AcceptLanguage?
            .Where(value => value.Quality.GetValueOrDefault(1) > 0)
            .OrderByDescending(value => value.Quality.GetValueOrDefault(1))
            .Select(value => value.Value.ToString().Split('-')[0])
            .FirstOrDefault(value => Languages.Supported.Contains(value, StringComparer.OrdinalIgnoreCase)) ?? Languages.Arabic;
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(Languages.FormattingCulture, language));
    }
}