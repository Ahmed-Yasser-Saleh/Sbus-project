using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;

namespace SBus.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse>(ILogger<TRequest> logger, IUser user)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMilliseconds = 500;

    private readonly ILogger<TRequest> _logger = logger;
    private readonly IUser _user = user;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (elapsedMilliseconds > SlowRequestThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {UserId}",
                typeof(TRequest).Name,
                elapsedMilliseconds,
                _user.Id ?? string.Empty);
        }

        return response;
    }
}
