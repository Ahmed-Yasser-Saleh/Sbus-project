using FluentValidation;

using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;
using SBus.Domain.Accounts;
using SBus.Tests.Common;

using Xunit;

namespace SBus.Api.Tests;

public sealed class MobileAuthServiceTests
{
    private readonly IMobileIdentity _identity = Substitute.For<IMobileIdentity>();
    private readonly IMobileRefreshTokenStore _store = Substitute.For<IMobileRefreshTokenStore>();
    private readonly IAccessTokenGenerator _access = Substitute.For<IAccessTokenGenerator>();
    private readonly IGoogleIdentityValidator _google = Substitute.For<IGoogleIdentityValidator>();
    private readonly IGoogleLinkProof _proof = Substitute.For<IGoogleLinkProof>();
    private readonly TimeProvider _clock = new FixedClock();
    private readonly MobileAccount _account = new("user", ["Traveler"], MobileAuthService.Hash("stamp"), true);

    [Fact]
    public async Task Login_PersistsOnlyHashAndIssuesBoundedTokens()
    {
        var service = Create();
        _identity.AuthenticateAsync("a@example.com", "password", Arg.Any<CancellationToken>()).Returns(_account);
        var response = await service.LoginAsync(new("a@example.com", "password"), default);
        await _store.Received(1).AddAsync(
            Arg.Is<MobileRefreshToken>(x => x.TokenHash == MobileAuthService.Hash(response.RefreshToken)
                && x.TokenHash != response.RefreshToken && x.SecurityStampHash == _account.SecurityStampHash), Arg.Any<CancellationToken>());
        Assert.Equal(_clock.GetUtcNow().AddMinutes(10), response.AccessExpiresAtUtc);
        Assert.Equal(_clock.GetUtcNow().AddDays(30), response.RefreshExpiresAtUtc);
        Assert.Equal(64, Convert.FromBase64String(response.RefreshToken).Length);
    }

    [Theory]
    [InlineData("invalid", "password")]
    [InlineData("a@example.com", "")]
    public async Task Login_InvalidFieldsNeverReachIdentity(string email, string password)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Create().LoginAsync(new(email, password), default));
        await _identity.DidNotReceiveWithAnyArgs().AuthenticateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Login_IdentityRejectionNeverPersistsSession()
    {
        _identity.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<MobileAccount>(_ => throw new UnauthenticatedException());
        await Assert.ThrowsAsync<UnauthenticatedException>(() => Create().LoginAsync(new("a@example.com", "password"), default));
        await _store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Refresh_RotatesAtomicallyWithoutExtendingFamilyExpiry()
    {
        var previous = Token();
        Prepare(previous);
        _store.RotateAsync(previous, Arg.Any<MobileRefreshToken>(), _clock.GetUtcNow(), Arg.Any<CancellationToken>()).Returns(true);
        var response = await Create().RefreshAsync(new("raw"), default);
        Assert.Equal(previous.ExpiresAtUtc, response.RefreshExpiresAtUtc);
        await _store.Received().RotateAsync(previous, Arg.Is<MobileRefreshToken>(x => x.FamilyId == previous.FamilyId
            && x.Id != previous.Id && x.TokenHash == MobileAuthService.Hash(response.RefreshToken)), _clock.GetUtcNow(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("reused")]
    [InlineData("stamp")]
    [InlineData("missing-user")]
    [InlineData("rotation-race")]
    public async Task Refresh_InvalidSessionRevokesFamily(string scenario)
    {
        var previous = Token();
        if (scenario == "expired")
        {
            previous = Token(_clock.GetUtcNow().AddSeconds(-1));
        }

        if (scenario == "reused")
        {
            previous.Revoke(_clock.GetUtcNow());
        }

        Prepare(previous);
        if (scenario == "stamp")
        {
            _identity.FindActiveAsync("user", Arg.Any<CancellationToken>()).Returns(_account with
            {
                SecurityStampHash = MobileAuthService.Hash("changed")
            });
        }

        if (scenario == "missing-user")
        {
            _identity.FindActiveAsync("user", Arg.Any<CancellationToken>()).Returns((MobileAccount?)null);
        }

        var exception = await Assert.ThrowsAsync<UnauthenticatedException>(() => Create().RefreshAsync(new("raw"), default));
        Assert.Equal(ErrorCodes.RefreshInvalid, exception.ErrorCode);
        await _store.Received().RevokeFamilyAsync("user", previous.FamilyId, _clock.GetUtcNow(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_UnknownTokenIsRejected()
    {
        var exception = await Assert.ThrowsAsync<UnauthenticatedException>(() => Create().RefreshAsync(new("unknown"), default));
        Assert.Equal(ErrorCodes.RefreshInvalid, exception.ErrorCode);
        await _store.DidNotReceiveWithAnyArgs().RotateAsync(default!, default!, default, default);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task JwtSession_RequiresCurrentStampAndLiveFamily(bool stampMatches, bool familyActive)
    {
        _identity.FindActiveAsync("user", Arg.Any<CancellationToken>()).Returns(_account);
        _store.IsFamilyActiveAsync("user", Arg.Any<Guid>(), _clock.GetUtcNow(), Arg.Any<CancellationToken>()).Returns(familyActive);
        var result = await Create().ValidateSessionAsync("user", MobileAuthService.Hash(stampMatches ? "stamp" : "old"), Guid.NewGuid(), default);
        Assert.Equal(stampMatches && familyActive, result is not null);
    }

    [Fact]
    public async Task JwtSession_DeletedOrLockedAccountIsRejected()
    {
        Assert.Null(await Create().ValidateSessionAsync("user", _account.SecurityStampHash, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Logout_RevokesOnlyAuthenticatedUsersFamily()
    {
        var family = Guid.NewGuid();
        await Create().LogoutAsync("user", family, default);
        await _store.Received().RevokeFamilyAsync("user", family, _clock.GetUtcNow(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MobileGoogleState.Ready)]
    [InlineData(MobileGoogleState.PasswordProofRequired)]
    [InlineData(MobileGoogleState.EmailConfirmationRequired)]
    public async Task Google_PreservesAccountResolutionStates(MobileGoogleState state)
    {
        var google = new GoogleIdentity("subject", "a@example.com", true, null, "Name");
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>()).Returns(google);
        _identity.ResolveGoogleAsync(google, Arg.Any<CancellationToken>()).Returns(new MobileGoogleAccount(state, state == MobileGoogleState.Ready ? _account : null));
        _proof.Protect(google).Returns("proof");
        var result = await Create().GoogleAsync(new("id-token"), default);
        Assert.Equal(state, result.State);
        Assert.Equal(state == MobileGoogleState.Ready, result.Tokens is not null);
        Assert.Equal(state == MobileGoogleState.PasswordProofRequired ? "proof" : null, result.LinkingProof);
    }

    [Fact]
    public async Task Google_InvalidProviderTokenNeverResolvesAccount()
    {
        _google.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<GoogleIdentity>(_ => throw new UnauthenticatedException());
        await Assert.ThrowsAsync<UnauthenticatedException>(() => Create().GoogleAsync(new("bad-token"), default));
        await _identity.DidNotReceiveWithAnyArgs().ResolveGoogleAsync(default!, default);
    }

    [Fact]
    public async Task GoogleLink_UsesProtectedIdentityAndPasswordProof()
    {
        var google = new GoogleIdentity("subject", "a@example.com", true, null, "Name");
        _proof.Unprotect("proof").Returns(google);
        _identity.LinkGoogleAsync(google, "password", Arg.Any<CancellationToken>()).Returns(_account);
        var response = await Create().LinkGoogleAsync(new("proof", "password"), default);
        Assert.Equal("user", response.Account.Id);
    }

    [Fact]
    public async Task GoogleLink_InvalidProofNeverReachesIdentity()
    {
        _proof.Unprotect("bad").Returns<GoogleIdentity>(_ => throw new UnauthenticatedException());
        await Assert.ThrowsAsync<UnauthenticatedException>(() => Create().LinkGoogleAsync(new("bad", "password"), default));
        await _identity.DidNotReceiveWithAnyArgs().LinkGoogleAsync(default!, default!, default);
    }

    private MobileAuthService Create() => new(_identity, _store, _access, _google, _proof, _clock, Options.Create(new MobileAuthOptions()));
    private MobileRefreshToken Token(DateTimeOffset? expires = null) => MobileRefreshToken.Create("user", Guid.NewGuid(), MobileAuthService.Hash("raw"), _account.SecurityStampHash,
        _clock.GetUtcNow().AddDays(-1), expires ?? _clock.GetUtcNow().AddDays(2));
    private void Prepare(MobileRefreshToken token)
    {
        _store.FindAsync(MobileAuthService.Hash("raw"), Arg.Any<CancellationToken>()).Returns(token);
        _identity.FindActiveAsync("user", Arg.Any<CancellationToken>()).Returns(_account);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 13, 10, 0, 0, TimeSpan.Zero);
    }
}