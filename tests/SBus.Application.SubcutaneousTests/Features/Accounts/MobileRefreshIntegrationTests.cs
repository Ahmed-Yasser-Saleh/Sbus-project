using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SBus.Application.Common.Exceptions;
using SBus.Application.Features.MobileAuth;
using SBus.Application.SubcutaneousTests.Common;
using SBus.Domain.Accounts;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Accounts;

[Collection(WebAppFactoryCollection.Name)]
public sealed class MobileRefreshIntegrationTests(WebAppFactory factory)
{
    [DatabaseFact]
    public async Task AppliedMigration_MatchesCurrentModelAndIsRecorded()
    {
        await using var db = factory.CreateDbContext();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains("20261013050141_AddMobileRefreshTokens", await db.Database.GetAppliedMigrationsAsync());
        var entity = db.Model.FindEntityType(typeof(MobileRefreshToken))!;
        Assert.Equal(10, entity.GetProperties().Count());
        Assert.Equal("uuid", entity.FindProperty(nameof(MobileRefreshToken.FamilyId))!.GetColumnType());
        Assert.Equal("character varying(450)", entity.FindProperty(nameof(MobileRefreshToken.UserId))!.GetColumnType());
        Assert.Equal("character varying(64)", entity.FindProperty(nameof(MobileRefreshToken.TokenHash))!.GetColumnType());
        Assert.Equal("character varying(64)", entity.FindProperty(nameof(MobileRefreshToken.SecurityStampHash))!.GetColumnType());
        Assert.Equal("timestamp with time zone", entity.FindProperty(nameof(MobileRefreshToken.ExpiresAtUtc))!.GetColumnType());
        Assert.Equal("bigint", entity.FindProperty(nameof(MobileRefreshToken.Version))!.GetColumnType());
        Assert.True(entity.FindProperty(nameof(MobileRefreshToken.Version))!.IsConcurrencyToken);
        Assert.Equal(3, entity.GetIndexes().Count());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.GetDatabaseName() == "IX_MobileRefreshTokens_TokenHash");
        Assert.All(entity.GetForeignKeys(), foreign => Assert.Equal(DeleteBehavior.Restrict, foreign.DeleteBehavior));
    }

    [DatabaseFact]
    public async Task LoginRefreshLogout_UsesRealIdentityPersistenceAndJwtPipeline()
    {
        var email = await CreateUserAsync();
        using var client = Client();
        var session = await LoginAsync(client, email);
        await using var db = factory.CreateDbContext();
        var stored = await db.Set<MobileRefreshToken>().SingleAsync(x => x.TokenHash == MobileAuthService.Hash(session.RefreshToken));
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(session.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var next = (await refreshed.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
        Assert.NotEqual(session.RefreshToken, next.RefreshToken);
        Assert.Equal(session.RefreshExpiresAtUtc, next.RefreshExpiresAtUtc);
        client.DefaultRequestHeaders.Authorization = new("Bearer", next.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(next.RefreshToken))).StatusCode);
    }

    [DatabaseFact]
    public async Task ReusedRefreshToken_RevokesReplacementAndAccessToken()
    {
        using var client = Client();
        var session = await LoginAsync(client, await CreateUserAsync());
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(session.RefreshToken));
        var next = (await response.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", next.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(next.RefreshToken))).StatusCode);
    }

    [DatabaseFact]
    public async Task PasswordReset_InvalidatesAccessAndRefreshUsingRealSecurityStamp()
    {
        var email = await CreateUserAsync();
        using var client = Client();
        var session = await LoginAsync(client, email);
        using (var scope = factory.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            Assert.True((await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), "new-password")).Succeeded);
        }

        client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode);
    }

    [DatabaseFact]
    public async Task ConcurrentRefreshes_ExactlyOneRotatesAndReuseRevokesFamily()
    {
        using var client = Client();
        var session = await LoginAsync(client, await CreateUserAsync());
        var attempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            using var scope = factory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IMobileAuthService>();
            try
            {
                return await service.RefreshAsync(new(session.RefreshToken), default);
            }
            catch (UnauthenticatedException)
            {
                return null;
            }
        });
        var results = await Task.WhenAll(attempts);
        var winner = Assert.Single(results, result => result is not null)!;
        await using var db = factory.CreateDbContext();
        var original = await db.Set<MobileRefreshToken>().SingleAsync(x => x.TokenHash == MobileAuthService.Hash(session.RefreshToken));
        Assert.False(await new MobileRefreshTokenStore(db).IsFamilyActiveAsync(original.UserId, original.FamilyId, factory.Clock.GetUtcNow(), default));
        Assert.NotNull(original.ReplacedByTokenId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(winner.RefreshToken))).StatusCode);
    }

    [DatabaseFact]
    public async Task ConcurrentLogoutAndRotation_CannotResurrectSession()
    {
        using var client = Client();
        var session = await LoginAsync(client, await CreateUserAsync());
        await using var db = factory.CreateDbContext();
        var original = await db.Set<MobileRefreshToken>().SingleAsync(x => x.TokenHash == MobileAuthService.Hash(session.RefreshToken));
        using var refreshScope = factory.CreateScope();
        using var logoutScope = factory.CreateScope();
        var refresh = refreshScope.ServiceProvider.GetRequiredService<IMobileAuthService>();
        var logout = logoutScope.ServiceProvider.GetRequiredService<IMobileAuthService>();
        var rotation = Task.Run(async () =>
        {
            try
            {
                await refresh.RefreshAsync(new(session.RefreshToken), default);
            }
            catch (UnauthenticatedException)
            {
                // Concurrent logout may revoke the session before rotation starts.
            }
        });
        await Task.WhenAll(rotation, logout.LogoutAsync(original.UserId, original.FamilyId, default));
        Assert.False(await new MobileRefreshTokenStore(db).IsFamilyActiveAsync(original.UserId, original.FamilyId, factory.Clock.GetUtcNow(), default));
    }

    [DatabaseFact]
    public async Task HashUniquenessAndRestrictiveForeignKeys_AreEnforcedByPostgres()
    {
        var email = await CreateUserAsync();
        using var scope = factory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = (await users.FindByEmailAsync(email))!;
        var now = factory.Clock.GetUtcNow();
        var hash = MobileAuthService.Hash(Guid.NewGuid().ToString());
        await using (var db = factory.CreateDbContext())
        {
            var store = new MobileRefreshTokenStore(db);
            await store.AddAsync(MobileRefreshToken.Create(user.Id, Guid.NewGuid(), hash, MobileAuthService.Hash("stamp"), now, now.AddDays(1)), default);
            await Assert.ThrowsAsync<UniqueConstraintException>(() => store.AddAsync(MobileRefreshToken.Create(user.Id, Guid.NewGuid(), hash, MobileAuthService.Hash("stamp"), now, now.AddDays(1)), default));
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => users.DeleteAsync(user));
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private async Task<string> CreateUserAsync()
    {
        using var scope = factory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"mobile-{Guid.NewGuid():N}@example.com";
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Test Traveler", PhoneNumber = "+201012345678" };
        Assert.True((await users.CreateAsync(user, "test-password")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, Roles.Traveler)).Succeeded);
        return email;
    }

    private static async Task<SessionResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "test-password"));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
    }
}
