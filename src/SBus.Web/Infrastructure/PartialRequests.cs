using Microsoft.AspNetCore.Http;

namespace SBus.Web.Infrastructure;

public static class PartialRequests
{
    public const string Header = "X-SBus-Region";

    public static bool Prepare(HttpContext context, string region)
    {
        context.Response.Headers.Append("Vary", Header);
        if (context.Request.Headers[Header] != region)
        {
            return false;
        }

        context.Response.Headers[Header] = region;
        context.Response.Headers.CacheControl = "no-store";
        return true;
    }
}
