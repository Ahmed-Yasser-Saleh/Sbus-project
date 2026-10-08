using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Settings;
using SBus.Application.Features.Trips.Commands.GenerateTrips;
using SBus.Infrastructure.Settings;

namespace SBus.Infrastructure.BackgroundJobs;

public class TripGenerationService(
    IServiceScopeFactory scopeFactory,
    ILogger<TripGenerationService> logger,
    IOptions<AppSettings> appSettings,
    IOptions<BookingOptions> bookingOptions) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<TripGenerationService> _logger = logger;
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly BookingOptions _bookingOptions = bookingOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.TripGenerationIntervalMinutes));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                await sender.Send(new GenerateTripsCommand(_bookingOptions.BookingWindowDays), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error generating trips.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
