using System.Net;
using System.Text.RegularExpressions;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using SBus.Application.Common.Interfaces;
using SBus.Application.Features.Accounts.Dtos;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Routes.Dtos;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetTripForBooking;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;
using SBus.Web.Infrastructure;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Web;

public class BookingPassengerTests
{
    private static readonly Guid TripId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PickupId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DropoffId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly string Path = $"/trip/{TripId}?date=2030-01-01";

    [Theory]
    [InlineData("ar", "rtl")]
    [InlineData("en", "ltr")]
    public async Task Booking_RendersInlineActionsAndUncheckedProfileOption(string language, string direction)
    {
        await using var factory = new BookingFactory();
        using var client = Client(factory);
        await client.GetAsync($"/Language?lang={language}&returnUrl=/");
        var html = await client.GetStringAsync(Path);
        Assert.Contains($"dir=\"{direction}\"", html);
        Assert.Contains("data-passenger-edit=\"name\"", html);
        Assert.Contains("data-passenger-edit=\"phone\"", html);
        Assert.DoesNotContain("data-passenger-change", html);
        Assert.DoesNotContain("data-passenger-cancel", html);
        Assert.DoesNotContain("asp-route-returnUrl", html);
        Assert.Contains("data-partial=\"booking-content\"", html);
        var accountOption = Regex.Match(html, "<input[^>]*data-passenger-update-account[^>]*>").Value;
        Assert.DoesNotContain("checked", accountOption);
        Assert.Contains("disabled", accountOption);
        Assert.Contains("value=\"Profile Name\"", html);
        Assert.Contains("readonly", Regex.Match(html, "<input[^>]*data-passenger-name(?:\\s|=)[^>]*>").Value);
    }

    [Fact]
    public async Task InvalidPassenger_RendersErrorsAndKeepsAllSelectionsInFragment()
    {
        await using var factory = new BookingFactory();
        factory.Sender.Send(Arg.Any<CreateOnlineBookingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<BookingCreatedDto>>(new List<Error>
            {
                Error.Validation("PassengerName", "اكتب اسمك بالكامل."),
                Error.Validation("PhoneNumber", "رقم الموبايل لازم يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015."),
            }));
        using var client = Client(factory);
        var token = Token(await client.GetStringAsync(Path));
        client.DefaultRequestHeaders.Add(PartialRequests.Header, "booking-content");
        using var form = Form(token, true, true, " ", "12345");
        var response = await client.PostAsync(Path, form);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("booking-content", response.Headers.GetValues(PartialRequests.Header).Single());
        Assert.DoesNotContain("<html", html);
        Assert.Contains("field-validation-error", html);
        Assert.Contains("value=\"12345\"", html);
        Assert.Contains("value=\"1\" checked", html);
        Assert.Contains("selected", Regex.Match(html, $"<option[^>]*value=\"{PickupId}\"[^>]*>").Value);
        Assert.Contains("selected", Regex.Match(html, $"<option[^>]*value=\"{DropoffId}\"[^>]*>").Value);
        Assert.Contains("date=2030-01-01", html);
        Assert.DoesNotContain("readonly", Regex.Match(html, "<input[^>]*data-passenger-name(?:\\s|=)[^>]*>").Value);
        Assert.Contains("checked", Regex.Match(html, "<input[^>]*data-passenger-update-account[^>]*>").Value);
    }

    [Theory]
    [InlineData(false, true, null, false)]
    [InlineData(true, false, "Family Member", false)]
    [InlineData(true, true, "Family Member", true)]
    public async Task Booking_OnlySendsOverridesAndProfileOptInWhileEditing(
        bool editing, bool checkbox, string? expectedName, bool expectedUpdate)
    {
        await using var factory = new BookingFactory();
        factory.Sender.Send(Arg.Any<CreateOnlineBookingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<BookingCreatedDto>>(new BookingCreatedDto(Guid.NewGuid(), "created-ticket")));
        using var client = Client(factory);
        var token = Token(await client.GetStringAsync(Path));
        using var form = Form(token, editing, checkbox, "Family Member", "01012345678");
        var response = await client.PostAsync(Path, form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/t/created-ticket", response.Headers.Location!.OriginalString);
        await factory.Sender.Received(1).Send(
            Arg.Is<CreateOnlineBookingCommand>(c =>
            c.PassengerName == expectedName && c.UpdateAccountDetails == expectedUpdate
            && c.PhoneNumber == (editing ? "01012345678" : null)
            && c.PickupStopId == PickupId && c.DropoffStopId == DropoffId && c.SeatNumbers.SequenceEqual(new[] { 1 })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncompleteProfile_RemainsBlockedBeforeBooking()
    {
        await using var factory = new BookingFactory();
        using var client = Client(factory);
        factory.Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Google Name", null));
        var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/CompleteProfile?", response.Headers.Location!.OriginalString);
        await factory.Sender.DidNotReceive().Send(Arg.Any<GetTripForBookingQuery>(), Arg.Any<CancellationToken>());
    }

    private static HttpClient Client(BookingFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });

    private static string Token(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    private static FormUrlEncodedContent Form(string token, bool editing, bool update, string name, string phone) => new(new Dictionary<string, string>
    {
        ["__RequestVerificationToken"] = token,
        ["Input.EditPassengerDetails"] = editing.ToString(),
        ["Input.EditPassengerName"] = editing.ToString(),
        ["Input.EditPassengerPhone"] = editing.ToString(),
        ["Input.UpdateAccountDetails"] = update.ToString(),
        ["Input.PassengerName"] = name,
        ["Input.PhoneNumber"] = phone,
        ["Input.PickupStopId"] = PickupId.ToString(),
        ["Input.DropoffStopId"] = DropoffId.ToString(),
        ["Input.SeatNumbers"] = "1",
    });

    private sealed class BookingFactory : PartialNavigationTests.PartialAppFactory
    {
        public ISender Sender { get; } = Substitute.For<ISender>();
        public ITravelerProfiles Profiles { get; } = Substitute.For<ITravelerProfiles>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                Profiles.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new TravelerProfileDto("Profile Name", "+201012345678"));
                Sender.Send(Arg.Any<GetTripForBookingQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Result<TripBookingDto>>(
                    new TripBookingDto(TripId, Direction.CairoToSuez, new DateOnly(2030, 1, 1), new TimeOnly(9, 0), 150, true,
                        new SeatMapDto(1, 2, [new SeatCellDto(1, 1, SeatCellKind.Seat, 1, false), new SeatCellDto(1, 2, SeatCellKind.Seat, 2, false)]),
                        [new RouteStopDto(PickupId, "القاهرة", 1, 0), new RouteStopDto(DropoffId, "السويس", 2, 90)])));
                services.AddSingleton(Sender);
                services.AddSingleton(Profiles);
            });
        }
    }
}
