using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Features.Bookings.Commands.ExpireOverdueHolds;
using SBus.Infrastructure.Settings;

namespace SBus.Infrastructure.BackgroundJobs;

public class HoldExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<HoldExpiryService> logger,
    IOptions<AppSettings> options) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<HoldExpiryService> _logger = logger;
    private readonly AppSettings _appSettings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_appSettings.HoldExpiryCheckSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                await sender.Send(new ExpireOverdueHoldsCommand(), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error expiring unpaid holds.");
            }
        }
    }
}
