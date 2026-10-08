using FluentValidation;

using MediatR;

using SBus.Application.Common.Behaviours;
using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Application.UnitTests.Behaviours;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task InvalidRequest_ReturnsValidationErrors_WithoutCallingHandler()
    {
        var behavior = new ValidationBehavior<CreateStopCommand, Result<StopDto>>(new CreateStopCommandValidator());
        var called = false;

        var result = await behavior.Handle(
            new CreateStopCommand(string.Empty, null),
            _ =>
            {
                called = true;
                return Task.FromResult<Result<StopDto>>(new StopDto(Guid.Empty, "x", null, true));
            },
            CancellationToken.None);

        Assert.False(called);
        Assert.True(result.IsError);
        Assert.All(result.Errors, e => Assert.Equal(ErrorKind.Validation, e.Type));
    }

    [Fact]
    public async Task ValidRequest_CallsHandler()
    {
        var behavior = new ValidationBehavior<CreateStopCommand, Result<StopDto>>(new CreateStopCommandValidator());
        var expected = new StopDto(Guid.CreateVersion7(), "رمسيس", null, true);

        var result = await behavior.Handle(
            new CreateStopCommand("رمسيس", null),
            _ => Task.FromResult<Result<StopDto>>(expected),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task NoValidator_CallsHandler()
    {
        var behavior = new ValidationBehavior<CreateStopCommand, Result<StopDto>>();
        var expected = new StopDto(Guid.CreateVersion7(), "x", null, true);

        var result = await behavior.Handle(
            new CreateStopCommand(string.Empty, null),
            _ => Task.FromResult<Result<StopDto>>(expected),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
