using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetMyTickets;
using SBus.Application.Features.Bookings.Queries.GetPendingPayments;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Application.Features.Routes.Dtos;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Application.Features.Schedules.Queries.GetSchedules;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;
using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Queries.GetStops;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetAvailableTrips;
using SBus.Application.Features.Trips.Queries.GetOfficeDay;
using SBus.Application.Features.Trips.Queries.GetTripManifest;
using SBus.Domain.Common.Results;
using SBus.Infrastructure.BackgroundJobs;
using SBus.Infrastructure.Identity;
using SBus.Web.Infrastructure;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Web;

public class PartialNavigationTests
{
    [Fact]
    public async Task Index_PartialUsesNormalizedSelectionAndOmitsLayout()
    {
        await using var factory = new PartialAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var full = await client.GetStringAsync("/");
        Assert.Contains("<html", full);
        Assert.Contains("data-partial=\"trips-list\"", full);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/?date=1900-01-01&direction=999");
        request.Headers.Add(PartialRequests.Header, "trips-list");
        using var response = await client.SendAsync(request);
        var partial = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("trips-list", response.Headers.GetValues(PartialRequests.Header).Single());
        Assert.Contains(PartialRequests.Header, response.Headers.Vary);
        Assert.DoesNotContain("<html", partial);
        Assert.DoesNotContain("class=\"hero\"", partial);
        Assert.Contains("direction=CairoToSuez", partial);
        Assert.DoesNotContain("1900-01-01", partial);
        Assert.Contains("board-row", partial);
    }

    [Theory]
    [InlineData("/Office")]
    [InlineData("/MyTickets?pageNumber=2")]
    [InlineData("/Office/Settings/Stops")]
    [InlineData("/Office/Settings/Drivers")]
    [InlineData("/Office/Settings/Buses")]
    [InlineData("/Office/Settings/Layouts")]
    [InlineData("/Office/Settings/Schedules")]
    [InlineData("/Office/Settings/Route?direction=SuezToCairo")]
    [InlineData("/Office/Trip/11111111-1111-1111-1111-111111111111")]
    public async Task OptedInPage_FetchOmitsLayoutButNormalRequestKeepsIt(string path)
    {
        await using var factory = new PartialAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Contains("<html", await client.GetStringAsync(path));
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(PartialRequests.Header, "page-content");
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"page-content\"", html);
        Assert.DoesNotContain("<html", html);
        Assert.Equal("page-content", response.Headers.GetValues(PartialRequests.Header).Single());
    }

    [Fact]
    public async Task PartialPost_AntiforgeryValidationAndSamePageRedirectArePreserved()
    {
        await using var factory = new PartialAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(PartialRequests.Header, "page-content");
        var html = await client.GetStringAsync("/Office/Settings/Stops");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        using var withoutToken = await client.PostAsync("/Office/Settings/Stops", new FormUrlEncodedContent(new Dictionary<string, string> { ["Name"] = "Test" }));
        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);

        using var invalid = await client.PostAsync("/Office/Settings/Stops", new FormUrlEncodedContent(new Dictionary<string, string> { ["Name"] = string.Empty, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("alert-danger", invalidHtml);
        Assert.DoesNotContain("<html", invalidHtml);

        using var valid = await client.PostAsync("/Office/Settings/Stops", new FormUrlEncodedContent(new Dictionary<string, string> { ["Name"] = "Test", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, valid.StatusCode);
        Assert.Equal("/Office/Settings/Stops", valid.Headers.Location?.OriginalString);
        var afterRedirect = await client.GetStringAsync(valid.Headers.Location);
        Assert.Contains("alert-success", afterRedirect);
        Assert.DoesNotContain("<html", afterRedirect);
    }

    [Fact]
    public async Task PartialRequest_UnauthorizedUserStillNavigatesToLogin()
    {
        await using var factory = new PartialAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(PartialRequests.Header, "page-content");
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using var response = await client.GetAsync("/Office");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        Assert.False(response.Headers.Contains(PartialRequests.Header));
    }

    public class PartialAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused");
            builder.UseSetting("Authentication:Email:PublicOrigin", "https://localhost");
            builder.UseSetting("Authentication:Email:Host", "unused");
            builder.UseSetting("Authentication:Email:From", "test@example.com");
            builder.ConfigureTestServices(services =>
            {
                foreach (var job in services.Where(d => d.ServiceType == typeof(IHostedService)
                    && (d.ImplementationType == typeof(HoldExpiryService) || d.ImplementationType == typeof(TripGenerationService))).ToList())
                {
                    services.Remove(job);
                }

                var sender = Substitute.For<ISender>();
                sender.Send(Arg.Any<GetAvailableTripsQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
                {
                    var query = call.Arg<GetAvailableTripsQuery>();
                    Result<List<TripSummaryDto>> result = new List<TripSummaryDto>
                    {
                        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), query.Direction, query.Date, new TimeOnly(9, 0), 150, 21, 21),
                    };
                    return Task.FromResult(result);
                });
                var stop = new StopDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Test stop", null, true);
                sender.Send(Arg.Any<GetStopsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<StopDto>>>(new List<StopDto> { stop }));
                sender.Send(Arg.Any<CreateStopCommand>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult<Result<StopDto>>(
                    string.IsNullOrWhiteSpace(call.Arg<CreateStopCommand>().Name) ? Error.Validation("Name", "Name required") : stop));
                sender.Send(Arg.Any<GetBusesQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<BusDto>>>(new List<BusDto>()));
                sender.Send(Arg.Any<GetDriversQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<DriverDto>>>(new List<DriverDto>()));
                sender.Send(Arg.Any<GetSeatLayoutsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<SeatLayoutDto>>>(new List<SeatLayoutDto>()));
                sender.Send(Arg.Any<GetSchedulesQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<ScheduleDto>>>(new List<ScheduleDto>()));
                sender.Send(Arg.Any<GetRouteQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<RouteStopDto>>>(new List<RouteStopDto>()));
                sender.Send(Arg.Any<GetOfficeDayQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<OfficeTripDto>>>(new List<OfficeTripDto>()));
                sender.Send(Arg.Any<GetPendingPaymentsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<List<PendingPaymentDto>>>(new List<PendingPaymentDto>()));
                sender.Send(Arg.Any<GetMyTicketsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<MyTicketDto>>([]));
                var manifest = new TripManifestDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), default, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(9, 0), 150, Guid.Empty, "Test", Guid.Empty, "Test", string.Empty, false, new SeatMapDto(0, 0, []), [], []);
                sender.Send(Arg.Any<GetTripManifestQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<TripManifestDto>>(manifest));
                services.RemoveAll<ISender>();
                services.AddSingleton(sender);
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "PartialTest";
                    options.DefaultChallengeScheme = "PartialTest";
                }).AddScheme<AuthenticationSchemeOptions, PartialTestAuthHandler>("PartialTest", _ => { });
            });
        }
    }

    public class PartialTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey("X-Test-Anonymous"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "partial-test"), new Claim(ClaimTypes.Role, Roles.Office), new Claim(ClaimTypes.Role, Roles.Traveler)], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        }
    }
}
