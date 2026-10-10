using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;
using SBus.Web.Services;
using Serilog;

namespace SBus.Web;

public static class DependencyInjection
{
    public const string OfficePolicy = AuthPolicies.LegacyOffice;

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
            .AddPolicy(OfficePolicy, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Office)
                .RequireAssertion(c => !c.User.IsInRole(Roles.CompanyOwner) && !c.User.IsInRole(Roles.CompanyEmployee)))
            .AddPolicy(AuthPolicies.Traveler, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Traveler)
                .RequireAssertion(c => !c.User.IsInRole(Roles.CompanyOwner) && !c.User.IsInRole(Roles.CompanyEmployee)))
            .AddPolicy(AuthPolicies.CompanyOwner, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.CompanyOwner))
            .AddPolicy(AuthPolicies.CompanyEmployee, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.CompanyEmployee))
            .AddPolicy(AuthPolicies.Company, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.CompanyOwner, Roles.CompanyEmployee));

        services.AddOptions<AccountEmailOptions>()
            .Bind(configuration.GetSection("Authentication:Email"))
            .Validate(
                o => Uri.TryCreate(o.PublicOrigin, UriKind.Absolute, out var uri)
                && uri.Scheme == "https" && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment) && string.IsNullOrEmpty(uri.UserInfo),
                "Authentication:Email:PublicOrigin must be an HTTPS origin.")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Host) && System.Net.Mail.MailAddress.TryCreate(o.From, out _)
                && o.Port is > 0 and <= 65535, "Configure SMTP Host, From and Port.")
            .ValidateOnStart();
        services.AddScoped<IAccountEmail, AccountEmail>();
        services.AddScoped<IRegistrationConfirmationSender, RegistrationConfirmationSender>();

        var google = configuration.GetSection("Authentication:Google");
        if (string.IsNullOrWhiteSpace(google["ClientId"]) != string.IsNullOrWhiteSpace(google["ClientSecret"]))
        {
            throw new InvalidOperationException("Configure both Google ClientId and ClientSecret, or neither.");
        }

        if (!string.IsNullOrWhiteSpace(google["ClientId"]) && !string.IsNullOrWhiteSpace(google["ClientSecret"]))
        {
            services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.UsePkce = true;
                options.SaveTokens = false;
                options.ClaimActions.MapJsonKey(TravelerAccounts.VerifiedEmailClaim, "verified_email");
                options.ClaimActions.MapJsonKey(TravelerAccounts.VerifiedEmailClaim, "email_verified");
                options.ClaimActions.MapJsonKey(TravelerAccounts.HostedDomainClaim, "hd");
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    context.Response.Redirect("/Account/Login?externalError=true");
                    return Task.CompletedTask;
                };
            });
        }

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });
        services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
        services.AddRazorPages(options => options.Conventions.ConfigureFilter(
            new Microsoft.AspNetCore.Mvc.ResponseCacheAttribute { NoStore = true, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None }));

        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "__Host-sbus.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
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
            options.AddPolicy(RateLimitPolicies.Account, httpContext =>
                HttpMethods.IsPost(httpContext.Request.Method)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(10),
                            QueueLimit = 0,
                        })
                    : RateLimitPartition.GetNoLimiter("account-reads"));
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

        app.UseRouting();

        app.Use(async (context, next) =>
        {
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            await next(context);
        });

        app.UseRateLimiter();

        app.UseAuthentication();

        app.UseAuthorization();

        return app;
    }
}
