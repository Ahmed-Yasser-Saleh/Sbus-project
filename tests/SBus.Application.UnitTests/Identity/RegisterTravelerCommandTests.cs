using MediatR;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts.Commands.RegisterTraveler;
using SBus.Domain.Common.Results;

using Xunit;

namespace SBus.Application.UnitTests.Identity;

public class RegisterTravelerCommandTests
{
    [Theory]
    [InlineData(null, "StrongPassword1!", "StrongPassword1!", "Email")]
    [InlineData("test@gmi", "StrongPassword1!", "StrongPassword1!", "Email")]
    [InlineData("person@example.com", null, null, "Password")]
    [InlineData("person@example.com", "1234", "1234", "Password")]
    [InlineData("person@example.com", "StrongPassword1!", "different", "ConfirmPassword")]
    public async Task InvalidCommand_StopsBeforeRegistrationAndEmail(
        string? email, string? password, string? confirmation, string field)
    {
        var registration = Substitute.For<ITravelerRegistration>();
        var sender = Substitute.For<IRegistrationConfirmationSender>();
        await using var provider = Services(registration, sender);
        var mediator = provider.GetRequiredService<ISender>();

        var result = await mediator.Send(new RegisterTravelerCommand(email, password, confirmation));

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == field);
        await registration.DidNotReceive().RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("new-user")]
    [InlineData(null)]
    public async Task Registration_OnlySucceedsWhenAccountWasCreated(string? userId)
    {
        var registration = Substitute.For<ITravelerRegistration>();
        var sender = Substitute.For<IRegistrationConfirmationSender>();
        registration.RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TravelerRegistrationOutcome>>(new TravelerRegistrationOutcome(userId)));
        await using var provider = Services(registration, sender);

        var result = await provider.GetRequiredService<ISender>()
            .Send(new RegisterTravelerCommand("person@example.com", "StrongPassword1!", "StrongPassword1!"));

        Assert.Equal(userId is not null, result.IsSuccess);
        await registration.Received(1).RegisterAsync("person@example.com", "StrongPassword1!", Arg.Any<CancellationToken>());
        if (userId is not null)
        {
            await sender.Received(1).SendAsync(userId, Arg.Any<CancellationToken>());
        }
        else
        {
            Assert.Equal("Email", result.TopError.Code);
            await sender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task IdentityPasswordPolicyError_ReturnsFieldErrorWithoutSendingEmail()
    {
        var registration = Substitute.For<ITravelerRegistration>();
        var sender = Substitute.For<IRegistrationConfirmationSender>();
        registration.RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TravelerRegistrationOutcome>>(Error.Validation("Password", "Password policy failed.")));
        await using var provider = Services(registration, sender);

        var result = await provider.GetRequiredService<ISender>()
            .Send(new RegisterTravelerCommand("person@example.com", "StrongPassword1!", "StrongPassword1!"));

        Assert.True(result.IsError);
        Assert.Equal("Password", result.TopError.Code);
        await sender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CommandLogging_DoesNotExposeCredentials()
    {
        var command = new RegisterTravelerCommand("person@example.com", "SecretPassword1!", "SecretPassword1!");
        Assert.Equal(nameof(RegisterTravelerCommand), command.ToString());
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcde")]
    [InlineData("!!!!!")]
    public async Task FiveCharacterPassword_DoesNotRequireComplexity(string password)
    {
        var validator = new RegisterTravelerCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterTravelerCommand("person@example.com", password, password));

        Assert.True(result.IsValid);
    }

    private static ServiceProvider Services(ITravelerRegistration registration, IRegistrationConfirmationSender sender)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(registration);
        services.AddSingleton(sender);
        services.AddSingleton(Substitute.For<IUser>());
        return services.BuildServiceProvider();
    }
}

