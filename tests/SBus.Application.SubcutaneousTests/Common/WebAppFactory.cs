using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using SBus.Infrastructure.BackgroundJobs;
using SBus.Infrastructure.Data;
using SBus.Tests.Common;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Common;

public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly DateTimeOffset StartTime = new(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; } = new(StartTime);

    public TestWorld World { get; private set; } = null!;

    public IServiceScope CreateScope() => Services.CreateScope();

    public IMediator CreateMediator() => CreateScope().ServiceProvider.GetRequiredService<IMediator>();

    public AppDbContext CreateDbContext() => CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    public async Task InitializeAsync()
    {
        if (TestDatabase.ConnectionString is null)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        World = await TestWorld.CreateAsync(context);
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestDatabase.ConnectionString ?? "Host=unused");
        builder.UseSetting("AppSettings:ReceiptsPath", Path.Combine(Path.GetTempPath(), "sbus-tests-receipts"));
        builder.UseSetting("Booking:HoldMinutes", "15");
        builder.UseSetting("Booking:PassengerCancellationCutoffHours", "2");
        builder.UseSetting("Booking:BookingWindowDays", "7");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            var jobs = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && (d.ImplementationType == typeof(HoldExpiryService) || d.ImplementationType == typeof(TripGenerationService)))
                .ToList();

            foreach (var job in jobs)
            {
                services.Remove(job);
            }
        });
    }
}
