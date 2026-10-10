using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Validation;
using SBus.Infrastructure.BackgroundJobs;
using SBus.Infrastructure.Data;
using SBus.Infrastructure.Data.Interceptors;
using SBus.Infrastructure.Identity;
using SBus.Infrastructure.Services;
using SBus.Infrastructure.Settings;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.Configure<BookingOptions>(configuration.GetSection(BookingOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();

        services
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = AccountPasswordPolicy.MinLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = configuration.GetValue("Authentication:RequireConfirmedEmail", true);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(2));
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.AddScoped<IIdentityTransaction, IdentityTransaction>();
        services.AddScoped<TravelerAccounts>();
        services.AddScoped<ITravelerRegistration>(sp => sp.GetRequiredService<TravelerAccounts>());

        services.AddSingleton<IReceiptStorage, LocalReceiptStorage>();

        services.AddHostedService<HoldExpiryService>();
        services.AddHostedService<TripGenerationService>();

        return services;
    }
}
