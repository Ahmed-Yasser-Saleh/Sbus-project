using System.Security.Cryptography;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts;
using SBus.Application.Features.MobileAuth;

namespace SBus.Api.Tests;

public sealed class ApiTestFactory(string environment = "Production", int authLimit = 100) : WebApplicationFactory<Program>
{
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public IMobileAuthService Auth { get; } = Substitute.For<IMobileAuthService>();
    public ISender Sender { get; } = Substitute.For<ISender>();
    public IAccountRecovery Recovery { get; } = Substitute.For<IAccountRecovery>();
    public ITravelerProfiles Profiles { get; } = Substitute.For<ITravelerProfiles>();

    public void Authenticate(HttpClient client, params string[] roles)
    {
        var account = new MobileAccount("user", roles, MobileAuthService.Hash("stamp"), true);
        Auth.ValidateSessionAsync("user", account.SecurityStampHash, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(account);
        var tokens = new SBus.Web.Api.Authentication.JwtAccessTokenGenerator(Microsoft.Extensions.Options.Options.Create(new MobileAuthOptions { SigningKey = SigningKey }));
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Generate(account, Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddMinutes(5)));
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=unused",
            ["AppSettings:InitializeDatabaseOnStartup"] = "false",
            ["Authentication:Email:Host"] = "unused",
            ["Authentication:Email:From"] = "tests@example.com",
            ["Authentication:Email:PublicOrigin"] = "https://localhost",
            ["Authentication:Google:ClientId"] = string.Empty,
            ["Authentication:Google:ClientSecret"] = string.Empty,
            ["Authentication:Mobile:Enabled"] = "true",
            ["Authentication:Mobile:SigningKey"] = SigningKey,
            ["Authentication:Mobile:AuthPermitLimit"] = authLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Authentication:Mobile:CorsOrigins:0"] = "https://mobile.example",
        };
        foreach (var setting in settings)
        {
            builder.UseSetting(setting.Key, setting.Value);
        }

        builder.ConfigureTestServices(services =>
        {
            foreach (var service in services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray())
            {
                services.Remove(service);
            }

            services.AddSingleton(Auth);
            services.AddSingleton(Sender);
            services.AddSingleton(Recovery);
            services.AddSingleton(Profiles);
            services.AddControllers().AddApplicationPart(typeof(ErrorProbeController).Assembly);
        });
    }
}