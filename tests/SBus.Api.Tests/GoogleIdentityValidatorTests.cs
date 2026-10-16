using Google.Apis.Auth;

using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;
using SBus.Infrastructure.Identity;

using Xunit;

namespace SBus.Api.Tests;

public sealed class GoogleIdentityValidatorTests
{
    [Fact]
    public async Task ProviderVerification_CancellationDoesNotStartNetworkRequest()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GoogleTokenVerifier().VerifyAsync("token", ["client"], cancellation.Token));
    }

    [Fact]
    public async Task ProviderVerification_UsesExplicitAudiencesAndVerifiedClaims()
    {
        var verifier = Substitute.For<IGoogleTokenVerifier>();
        var audiences = new[] { "android-client", "ios-client" };
        verifier.VerifyAsync("token", audiences, Arg.Any<CancellationToken>()).Returns(new GoogleJsonWebSignature.Payload
        {
            Subject = "subject",
            Email = "a@gmail.com",
            EmailVerified = true,
            Name = "Name",
            HostedDomain = "workspace.example",
        });
        var result = await new GoogleIdentityValidator(Options.Create(new MobileAuthOptions { GoogleClientIds = audiences }), verifier).ValidateAsync("token", default);
        Assert.Equal("subject", result.Subject);
        Assert.Equal("workspace.example", result.HostedDomain);
        await verifier.Received().VerifyAsync("token", audiences, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, "subject", "a@gmail.com")]
    [InlineData(true, "", "a@gmail.com")]
    [InlineData(true, "subject", "")]
    public async Task InvalidIdentityClaims_AreRejected(bool verified, string subject, string email)
    {
        var verifier = Substitute.For<IGoogleTokenVerifier>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(new GoogleJsonWebSignature.Payload
        {
            Subject = subject,
            Email = email,
            EmailVerified = verified,
        });
        var error = await Assert.ThrowsAsync<UnauthenticatedException>(() =>
            new GoogleIdentityValidator(Options.Create(new MobileAuthOptions { GoogleClientIds = ["client"] }), verifier).ValidateAsync("token", default));
        Assert.Equal(ErrorCodes.GoogleTokenInvalid, error.ErrorCode);
    }

    [Fact]
    public async Task InvalidSignatureOrAudience_UsesSafeError()
    {
        var verifier = Substitute.For<IGoogleTokenVerifier>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<GoogleJsonWebSignature.Payload>(_ => throw new InvalidJwtException("internal verification diagnostics"));
        var error = await Assert.ThrowsAsync<UnauthenticatedException>(() =>
            new GoogleIdentityValidator(Options.Create(new MobileAuthOptions { GoogleClientIds = ["client"] }), verifier).ValidateAsync("token", default));
        Assert.Equal(ErrorCodes.GoogleTokenInvalid, error.ErrorCode);
        Assert.DoesNotContain("internal", error.Message);
    }

    [Fact]
    public async Task UnconfiguredProvider_DoesNotCallVerifier()
    {
        var verifier = Substitute.For<IGoogleTokenVerifier>();
        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => new GoogleIdentityValidator(Options.Create(new MobileAuthOptions()), verifier).ValidateAsync("token", default));
        Assert.Equal(ErrorCodes.GoogleUnavailable, error.ErrorCode);
        await verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default);
    }
}