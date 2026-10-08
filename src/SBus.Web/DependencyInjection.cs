using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

using SBus.Application.Common.Interfaces;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;
using SBus.Web.Services;

using Serilog;

namespace SBus.Web;

public static class DependencyInjection
{
    public const string OfficePolicy = "Office";

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(Languages.FormattingCulture, Languages.Arabic);
            options.SupportedCultures = [new CultureInfo(Languages.FormattingCulture)];
            options.SupportedUICultures = [.. Languages.Supported.Select(l => new CultureInfo(l))];
            options.RequestCultureProviders = [new CookieRequestCultureProvider()];
        });

        services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeFolder("/Office", OfficePolicy);
            options.Conventions.AllowAnonymousToPage("/Office/Login");
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(OfficePolicy, policy => policy.RequireRole(Roles.Office));

        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
        {
            options.LoginPath = "/Office/Login";
            options.AccessDeniedPath = "/Office/Login";
            options.Cookie.Name = "sbus.office";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;
        });

        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 6 * 1024 * 1024);

        services.AddScoped<IUser, CurrentUser>();
        services.AddHttpContextAccessor();

        services.AddAppRateLimiting();

        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolicies.PublicWrites, httpContext =>
                HttpMethods.IsPost(httpContext.Request.Method)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(10),
                            QueueLimit = 0,
                        })
                    : RateLimitPartition.GetNoLimiter("reads"));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(localizer["محاولات كتير في وقت قصير. استنى شوية وجرب تاني."], ct);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseRateLimiter();

        app.UseAuthentication();

        app.UseAuthorization();

        return app;
    }
}
