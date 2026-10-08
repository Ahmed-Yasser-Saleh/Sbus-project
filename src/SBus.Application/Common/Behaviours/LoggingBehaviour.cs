using MediatR.Pipeline;

using Microsoft.Extensions.Logging;

using SBus.Application.Common.Interfaces;

namespace SBus.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest>(ILogger<TRequest> logger, IUser user)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger = logger;
    private readonly IUser _user = user;

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request: {Name} {UserId}", typeof(TRequest).Name, _user.Id ?? string.Empty);

        return Task.CompletedTask;
    }
}
