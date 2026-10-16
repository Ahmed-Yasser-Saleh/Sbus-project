using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Features.MobileAuth;
using SBus.Web.Api.Authentication;

using Xunit;

namespace SBus.Api.Tests;

public sealed class ApiPipelineTests
{
    [Theory]
    [InlineData("en", "must not be empty")]
    [InlineData("ar", "فارغ")]
    public async Task ApiErrors_RespectRequestedLanguage(string language, string expected)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        var problem = await AssertProblem(await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("invalid", string.Empty)), 400, "VALIDATION_FAILED");
        Assert.Contains(expected, problem.GetProperty("errors").GetProperty("password")[0].GetString());
    }

    [Theory]
    [InlineData("key")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expiry")]
    public async Task JwtValidation_RejectsInvalidSignatureIssuerAudienceAndLifetime(string failure)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var account = new MobileAccount("user", ["Traveler"], MobileAuthService.Hash("stamp"), true);
        var options = new MobileAuthOptions
        {
            SigningKey = failure == "key" ? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)) : factory.SigningKey,
            Issuer = failure == "issuer" ? "https://untrusted.example" : "https://sbus.app",
            Audience = failure == "audience" ? "other-app" : "sbus-mobile",
        };
        var now = DateTimeOffset.UtcNow;
        var expires = failure == "expiry" ? now.AddMinutes(-1) : now.AddMinutes(5);
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtAccessTokenGenerator(Options.Create(options))
            .Generate(account, Guid.NewGuid(), now.AddMinutes(-5), expires));
        await AssertProblem(await client.GetAsync("/api/v1/probe/traveler"), 401, "AUTH_UNAUTHENTICATED");
        await factory.Auth.DidNotReceiveWithAnyArgs().ValidateSessionAsync(default!, default!, default, default);
    }

    [Theory]
    [InlineData("validation", 400, "VALIDATION_FAILED", "validation")]
    [InlineData("not-found", 404, "TRIP_NOT_FOUND", "not-found")]
    [InlineData("conflict", 409, "BOOKING_SEAT_UNAVAILABLE", "conflict")]
    [InlineData("business-rule", 422, "TRAVELER_PROFILE_INCOMPLETE", "business-rule")]
    [InlineData("unauthenticated", 401, "AUTH_UNAUTHENTICATED", "authentication")]
    [InlineData("forbidden", 403, "AUTH_FORBIDDEN", "forbidden")]
    [InlineData("unavailable", 503, "API_UNAVAILABLE", "unavailable")]
    [InlineData("concurrency", 409, "CONCURRENCY_CONFLICT", "conflict")]
    [InlineData("unique", 409, "CONCURRENCY_CONFLICT", "conflict")]
    [InlineData("body-size", 413, "REQUEST_TOO_LARGE", "request")]
    [InlineData("unexpected", 500, "INTERNAL_ERROR", "internal")]
    public async Task Exceptions_ReturnConsistentProblemDetails(string category, int status, string code, string type)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var path = "/api/v1/probe/" + category;
        var response = await client.GetAsync(path);
        var problem = await AssertProblem(response, status, code);
        Assert.Equal("https://sbus.app/errors/" + type, problem.GetProperty("type").GetString());
        Assert.Equal(path, problem.GetProperty("instance").GetString());
        if (status >= 500)
        {
            Assert.DoesNotContain("private", problem.GetProperty("detail").GetString());
        }

        if (status == 400)
        {
            Assert.Equal("Invalid minutes.", problem.GetProperty("errors").GetProperty("stops[0].minutesFromStart")[0].GetString());
        }
    }

    [Fact]
    public async Task Development_UnexpectedExceptionIncludesDetail()
    {
        await using var factory = new ApiTestFactory("Development");
        using var client = factory.Client();
        var problem = await AssertProblem(await client.GetAsync("/api/v1/probe/unexpected"), 500, "INTERNAL_ERROR");
        Assert.Contains("password=private", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task JwtChallenge_Returns401ProblemAndBearerHeader()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var response = await client.PostAsync("/api/v1/auth/logout", null);
        await AssertProblem(response, 401, "AUTH_UNAUTHENTICATED");
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("traveler", "Traveler", 200)]
    [InlineData("owner", "CompanyOwner", 200)]
    [InlineData("employee", "CompanyEmployee", 200)]
    [InlineData("traveler", "CompanyOwner", 403)]
    [InlineData("traveler", "Traveler,CompanyEmployee", 403)]
    [InlineData("owner", "Traveler", 403)]
    public async Task JwtRoles_PreserveExistingPolicies(string endpoint, string roles, int expected)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        Authenticate(factory, client, roles.Split(','));
        var response = await client.GetAsync("/api/v1/probe/" + endpoint);
        if (expected == 403)
        {
            await AssertProblem(response, expected, "AUTH_FORBIDDEN");
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RoleChanges_AreAppliedOnNextJwtRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var account = Authenticate(factory, client, ["Traveler"]);
        factory.Auth.ValidateSessionAsync("user", account.SecurityStampHash, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(account with { Roles = ["CompanyOwner"] });
        await AssertProblem(await client.GetAsync("/api/v1/probe/traveler"), 403, "AUTH_FORBIDDEN");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidOrRevokedJwt_Returns401(bool revoked)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var account = Authenticate(factory, client, ["Traveler"]);
        if (revoked)
        {
            factory.Auth.ValidateSessionAsync("user", account.SecurityStampHash, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MobileAccount?)null);
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid");
        }

        await AssertProblem(await client.GetAsync("/api/v1/probe/traveler"), 401, "AUTH_UNAUTHENTICATED");
    }

    [Theory]
    [InlineData("{}", "email")]
    [InlineData("{\"email\":12,\"password\":\"pass\"}", "$.email")]
    [InlineData("{\"email\":\"bad\",\"password\":\"\"}", "email")]
    public async Task ModelBindingAndFluentValidation_UseFieldErrors(string json, string field)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var response = await client.PostAsync("/api/v1/auth/login", new StringContent(json, Encoding.UTF8, "application/json"));
        var problem = await AssertProblem(response, 400, "VALIDATION_FAILED");
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        await factory.Auth.DidNotReceiveWithAnyArgs().LoginAsync(default!, default);
    }

    [Fact]
    public async Task AuthRateLimit_ReturnsProblemWithRetryAfter()
    {
        await using var factory = new ApiTestFactory(authLimit: 1);
        using var client = factory.Client();
        await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("bad", string.Empty));
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("bad", string.Empty));
        await AssertProblem(response, 429, "RATE_LIMIT_EXCEEDED");
        Assert.True(response.Headers.RetryAfter!.Delta > TimeSpan.Zero);
    }

    [Theory]
    [InlineData("/api/v1/missing", 404, "API_ROUTE_NOT_FOUND")]
    [InlineData("/api/v2/auth/login", 404, "API_ROUTE_NOT_FOUND")]
    [InlineData("/api/v1/auth/login", 405, "METHOD_NOT_ALLOWED")]
    public async Task RoutingErrors_UseProblemDetails(string path, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await AssertProblem(await client.GetAsync(path), status, code);
    }

    [Fact]
    public async Task UnsupportedContentType_Returns415Problem()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await AssertProblem(await client.PostAsync("/api/v1/auth/login", new StringContent("email=foo")), 415, "UNSUPPORTED_MEDIA_TYPE");
    }

    [Fact]
    public async Task RazorAuthorization_StillRedirectsToLogin()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var response = await client.GetAsync("/MyTickets");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task RazorErrors_KeepHtmlErrorPageAndHideException()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        var response = await client.GetAsync("/web-error-probe");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("Web private diagnostics", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_ContainsBearerAndErrorSchemas()
    {
        await using var factory = new ApiTestFactory("Development");
        using var client = factory.Client();
        var json = await client.GetStringAsync("/api/docs/v1/swagger.json");
        Assert.Contains("Bearer", json);
        Assert.Contains("ValidationProblemDetails", json);
        Assert.Contains("/api/v1/auth/refresh", json);
        Assert.Contains("application/problem+json", json);
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/office/schedules/{id}", out _));
        var upload = paths.GetProperty("/api/v1/tickets/{token}/receipt").GetProperty("post");
        Assert.True(upload.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _));
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject().Where(x => x.Value.TryGetProperty("responses", out _)))
            {
                foreach (var response in operation.Value.GetProperty("responses").EnumerateObject().Where(x => int.Parse(x.Name, System.Globalization.CultureInfo.InvariantCulture) >= 400))
                {
                    Assert.Equal("application/problem+json", Assert.Single(response.Value.GetProperty("content").EnumerateObject()).Name);
                }
            }
        }
    }

    [Theory]
    [InlineData("https://mobile.example", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Cors_OnlyAllowsConfiguredOrigins(string origin, bool allowed)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static MobileAccount Authenticate(ApiTestFactory factory, HttpClient client, string[] roles)
    {
        var account = new MobileAccount("user", roles, MobileAuthService.Hash("stamp"), true);
        factory.Auth.ValidateSessionAsync("user", account.SecurityStampHash, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(account);
        var generator = new JwtAccessTokenGenerator(Options.Create(new MobileAuthOptions { SigningKey = factory.SigningKey }));
        client.DefaultRequestHeaders.Authorization = new("Bearer", generator.Generate(account, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddMinutes(5)));
        return account;
    }

    private static async Task<JsonElement> AssertProblem(HttpResponseMessage response, int status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode == status, $"Expected {status}, got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var document = JsonDocument.Parse(body);
        var problem = document.RootElement.Clone();
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        return problem;
    }
}