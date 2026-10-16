using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

using MediatR;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;
using SBus.Domain.Accounts;
using SBus.Infrastructure.Data;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Authentication;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Api.Tests;

public sealed class FoundationTests
{
    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 101, false)]
    [InlineData(10001, 20, false)]
    [InlineData(1, 20, true)]
    public void Pagination_RejectsUnboundedRequests(int page, int size, bool valid)
    {
        var request = new PageRequest { Page = page, PageSize = size };
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(request);
        Assert.Equal(valid, System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request, context, [], true));
    }

    [Fact]
    public void Jwt_SignsIssuerAudienceRoleStampSessionAndExpiry()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var options = new MobileAuthOptions { SigningKey = key };
        var account = new MobileAccount("user", [Roles.Traveler], MobileAuthService.Hash("stamp"), true);
        var family = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        var raw = new JwtAccessTokenGenerator(Options.Create(options)).Generate(account, family, now, now.AddMinutes(10));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(raw, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = MobileClaims.Role,
        }, out var validated);
        Assert.Equal("user", principal.FindFirst("sub")!.Value);
        Assert.Equal(account.SecurityStampHash, principal.FindFirst(MobileClaims.SecurityStampHash)!.Value);
        Assert.Equal(family.ToString(), principal.FindFirst(MobileClaims.FamilyId)!.Value);
        Assert.True(principal.IsInRole(Roles.Traveler));
        Assert.Equal(now.AddMinutes(10).ToUnixTimeSeconds(), new DateTimeOffset(validated.ValidTo).ToUnixTimeSeconds());
        Assert.DoesNotContain("stamp", raw);
    }

    [Theory]
    [InlineData("Traveler", false, "/", true)]
    [InlineData("Traveler", true, "/", false)]
    [InlineData("CompanyOwner", false, "/Account/ComingSoon", false)]
    [InlineData("CompanyEmployee", false, "/Account/ComingSoon", false)]
    [InlineData("Office", false, "/Office", false)]
    public void Mapper_PreservesRoleLandingAndProfileGate(string role, bool complete, string destination, bool needsProfile)
    {
        var account = new MobileAccount("user", [role], MobileAuthService.Hash("stamp"), complete);
        var result = SessionMapper.Map(new MobileTokens("access", "refresh", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), account));
        Assert.Equal(destination, result.Destination);
        Assert.Equal(needsProfile, result.RequiresProfileCompletion);
    }

    [Fact]
    public void GoogleProof_RoundTripsAndRejectsTampering()
    {
        var proof = new GoogleLinkProof(new EphemeralDataProtectionProvider());
        var identity = new GoogleIdentity("subject", "a@example.com", true, "example.com", "Name");
        var encrypted = proof.Protect(identity);
        Assert.Equal(identity, proof.Unprotect(encrypted));
        Assert.DoesNotContain("a@example.com", encrypted);
        Assert.Throws<UnauthenticatedException>(() => proof.Unprotect(encrypted + "tamper"));
    }

    [Fact]
    public void RefreshEntity_ExpiryAndRevocationAreIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var token = MobileRefreshToken.Create("user", Guid.NewGuid(), MobileAuthService.Hash("token"), MobileAuthService.Hash("stamp"), now, now.AddDays(1));
        Assert.True(token.IsActive(now));
        Assert.False(token.IsActive(now.AddDays(1)));
        var replacement = Guid.NewGuid();
        token.Revoke(now.AddMinutes(1), replacement);
        token.Revoke(now.AddMinutes(2));
        Assert.False(token.IsActive(now));
        Assert.Equal(1, token.Version);
        Assert.Equal(replacement, token.ReplacedByTokenId);
    }

    [Fact]
    public void RefreshConfiguration_MapsConstraintsWithoutConnectingToDatabase()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=unused").Options, Substitute.For<IMediator>());
        var entity = db.Model.FindEntityType(typeof(MobileRefreshToken))!;
        Assert.Equal("MobileRefreshTokens", entity.GetTableName());
        Assert.True(entity.FindProperty(nameof(MobileRefreshToken.Version))!.IsConcurrencyToken);
        Assert.Equal(64, entity.FindProperty(nameof(MobileRefreshToken.TokenHash))!.GetMaxLength());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(MobileRefreshToken.TokenHash));
        Assert.All(entity.GetForeignKeys(), foreign => Assert.Equal(DeleteBehavior.Restrict, foreign.DeleteBehavior));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void RefreshValidator_RejectsEmptyOrExcessiveToken(string input)
    {
        var token = input == "short" ? new string('a', 257) : input;
        Assert.False(new RefreshRequestValidator().Validate(new RefreshRequest(token)).IsValid);
    }

    [Fact]
    public void GoogleValidators_RejectEmptyAndOversizedInputs()
    {
        Assert.False(new GoogleRequestValidator().Validate(new GoogleRequest(string.Empty)).IsValid);
        Assert.False(new GoogleRequestValidator().Validate(new GoogleRequest(new string('a', 16385))).IsValid);
        Assert.False(new GoogleLinkRequestValidator().Validate(new GoogleLinkRequest(string.Empty, string.Empty)).IsValid);
        Assert.True(new GoogleLinkRequestValidator().Validate(new GoogleLinkRequest("proof", "password")).IsValid);
    }
}