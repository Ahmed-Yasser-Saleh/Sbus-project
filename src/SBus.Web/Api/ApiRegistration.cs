using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using SBus.Application.Features.MobileAuth;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Authentication;
using SBus.Web.Api.Errors;

namespace SBus.Web.Api;

public static class ApiRegistration
{
    public const string AuthRatePolicy = "MobileAuth";
    public const string CorsPolicy = "MobileApi";
    public const string RouteScheme = "SBusRoute";

    public static IServiceCollection AddMobileApi(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(MobileAuthOptions.SectionName).Get<MobileAuthOptions>() ?? new();
        services.AddOptions<MobileAuthOptions>().Bind(configuration.GetSection(MobileAuthOptions.SectionName))
            .Validate(o => !o.Enabled || Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "Mobile API needs a signing key of at least 32 bytes.")
            .Validate(
                o => !o.Enabled || (!string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience)
                && o.AccessTokenMinutes is >= 1 and <= 60 && o.RefreshTokenDays is >= 1 and <= 90
                && o.AuthPermitLimit > 0 && o.AuthWindowSeconds > 0), "Invalid mobile authentication settings.")
            .Validate(o => o.GoogleClientIds.All(x => !string.IsNullOrWhiteSpace(x)), "Google audience IDs must not be empty.")
            .Validate(
                o => o.CorsOrigins.All(x => Uri.TryCreate(x, UriKind.Absolute, out var uri)
                && uri.Scheme is "https" or "http" && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)), "CORS requires explicit origins.")
            .ValidateOnStart();

        services.AddScoped<IMobileAuthService, MobileAuthService>();
        services.AddScoped<IMobileIdentity, MobileIdentity>();
        services.AddScoped<IMobileRefreshTokenStore, MobileRefreshTokenStore>();
        services.AddScoped<IGoogleIdentityValidator, GoogleIdentityValidator>();
        services.AddSingleton<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddSingleton<IGoogleLinkProof, GoogleLinkProof>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddTransient<IProblemDetailsWriter, ApiJsonProblemWriter>();
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ApiProblems.Customize);
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddScoped<ApiResultAdapter>();
        services.AddScoped<CompleteApiProfileFilter>();
        services.AddScoped<SBus.Application.Features.Accounts.IAccountRecovery, SBus.Web.Services.AccountRecoveryService>();
        services.Configure<RequestLocalizationOptions>(o => o.RequestCultureProviders.Insert(0, new ApiCultureProvider()));
        services.AddControllers(o => o.Filters.Add<ApiValidationFilter>());
        services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Where(x => x.Value!.Errors.Count > 0).ToDictionary(
                x => ApiExceptionHandler.JsonField(x.Key), x => x.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                    ? "The supplied value is invalid." : e.ErrorMessage).ToArray());
            var problem = new ValidationProblemDetails(errors) { Status = 400 };
            ApiProblems.Customize(new ProblemDetailsContext { HttpContext = context.HttpContext, ProblemDetails = problem });
            context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ApiValidation")
                .LogInformation("API model validation rejected TraceId {TraceId}", problem.Extensions["traceId"]);
            return new ObjectResult(problem) { StatusCode = 400, ContentTypes = { ApiProblems.ContentType } };
        });

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = RouteScheme;
            o.DefaultChallengeScheme = RouteScheme;
            o.DefaultForbidScheme = RouteScheme;
        }).AddPolicyScheme(RouteScheme, RouteScheme, o => o.ForwardDefaultSelector = http => ApiProblems.IsApi(http)
            ? JwtBearerDefaults.AuthenticationScheme : IdentityConstants.ApplicationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.IncludeErrorDetails = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    IssuerSigningKey = settings.Enabled ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)) : null,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = MobileClaims.Role,
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        return ApiProblems.WriteAsync(context.HttpContext, 401);
                    },
                    OnForbidden = context => ApiProblems.WriteAsync(context.HttpContext, 403),
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        var stamp = principal.FindFirstValue(MobileClaims.SecurityStampHash);
                        if (userId is null || stamp is null || !Guid.TryParse(principal.FindFirstValue(MobileClaims.FamilyId), out var familyId))
                        {
                            context.Fail("Invalid session.");
                            return;
                        }

                        var account = await context.HttpContext.RequestServices.GetRequiredService<IMobileAuthService>()
                            .ValidateSessionAsync(userId, stamp, familyId, context.HttpContext.RequestAborted);
                        if (account is null)
                        {
                            context.Fail("Revoked session.");
                            return;
                        }

                        var identity = (ClaimsIdentity)principal.Identity!;
                        foreach (var role in identity.FindAll(MobileClaims.Role).ToArray())
                        {
                            identity.RemoveClaim(role);
                        }

                        identity.AddClaims(account.Roles.Select(role => new Claim(MobileClaims.Role, role)));
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
                    },
                };
            });

        services.AddCors(o => o.AddPolicy(CorsPolicy, policy =>
        {
            if (settings.CorsOrigins.Length > 0)
            {
                policy.WithOrigins(settings.CorsOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        }));
        services.AddRateLimiter(o =>
        {
            o.AddPolicy(AuthRatePolicy, http => HttpMethods.IsPost(http.Request.Method)
                ? RateLimitPartition.GetFixedWindowLimiter(http.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, settings.AuthPermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, settings.AuthWindowSeconds)),
                    QueueLimit = 0,
                }) : RateLimitPartition.GetNoLimiter("mobile-auth-reads"));
            var webRejected = o.OnRejected;
            o.OnRejected = async (context, ct) =>
            {
                if (!ApiProblems.IsApi(context.HttpContext))
                {
                    if (webRejected is not null)
                    {
                        await webRejected(context, ct);
                    }

                    return;
                }

                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : Math.Max(1, settings.AuthWindowSeconds);
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await ApiProblems.WriteAsync(context.HttpContext, 429);
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.OperationFilter<ApiProblemOperationFilter>();
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "S Bus Mobile API", Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Use a mobile access token.",
            });
            o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });
        return services;
    }

    public static IApplicationBuilder UseMobileApiErrors(this IApplicationBuilder app) => app.UseWhen(ApiProblems.IsApi, branch =>
    {
        branch.UseExceptionHandler();
        branch.UseStatusCodePages(context => ApiProblems.WriteAsync(context.HttpContext, context.HttpContext.Response.StatusCode));
        branch.Use(async (http, next) =>
        {
            if (!http.RequestServices.GetRequiredService<IOptions<MobileAuthOptions>>().Value.Enabled)
            {
                await ApiProblems.WriteAsync(http, 503);
                return;
            }

            await next(http);
        });
    });
}
