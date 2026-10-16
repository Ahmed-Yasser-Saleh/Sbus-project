using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.MobileAuth;
using SBus.Application.SubcutaneousTests.Common;
using SBus.Domain.Bookings;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Bookings;

[Collection(WebAppFactoryCollection.Name)]
public sealed class MobileBookingIntegrationTests(WebAppFactory database)
{
    [DatabaseFact]
    public async Task BookingReceiptCancellation_UseRealCurrentUserAndHideOtherTravelers()
    {
        await using var host = new WebAppFactory { UseHttpUser = true };
        using var owner = Client(host);
        using var stranger = Client(host);
        using var anonymous = Client(host);
        await Login(host, owner);
        await Login(host, stranger);
        var original = await owner.GetFromJsonAsync<ApiResponse<SBus.Application.Features.Accounts.Dtos.TravelerProfileDto>>("/api/v1/me/profile");
        Assert.Equal("Test Traveler", original!.Data.FullName);
        var profile = await owner.PutAsJsonAsync("/api/v1/me/profile", new { fullName = "Updated Traveler", phoneNumber = "01112345678" });
        Assert.True(profile.IsSuccessStatusCode, await profile.Content.ReadAsStringAsync());
        var saved = await owner.GetFromJsonAsync<ApiResponse<SBus.Application.Features.Accounts.Dtos.TravelerProfileDto>>("/api/v1/me/profile");
        Assert.Equal("Updated Traveler", saved!.Data.FullName);
        Assert.Equal("+201112345678", saved.Data.PhoneNumber);
        await using var db = database.CreateDbContext();
        var trip = await database.World.CreateTripAsync(db, database.Clock.GetUtcNow().AddDays(2));
        var command = new CreateOnlineBookingCommand(trip, database.World.FirstStopId, database.World.LastStopId, [1]);
        var response = await owner.PostAsJsonAsync("/api/v1/bookings", command);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var booking = (await response.Content.ReadFromJsonAsync<ApiResponse<BookingCreatedDto>>())!.Data;
        var path = "/api/v1/tickets/" + booking.PublicToken;
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync(path + "/cancel", null)).StatusCode);
        using var receipt = new MultipartFormDataContent();
        receipt.Add(new ByteArrayContent([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0x0d]), "receipt", "receipt.png");
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync(path + "/receipt", receipt)).StatusCode);
        var ticket = (await owner.GetFromJsonAsync<ApiResponse<TicketDto>>(path))!.Data;
        Assert.Equal(BookingStatus.AwaitingConfirmation, ticket.Status);
        Assert.Equal("Updated Traveler", ticket.PassengerName);
        Assert.Null(ticket.DriverName);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync(path + "/cancel", null)).StatusCode);
        Assert.Equal(BookingStatus.Cancelled, (await owner.GetFromJsonAsync<ApiResponse<TicketDto>>(path))!.Data.Status);
        var mine = await owner.GetFromJsonAsync<PagedResponse<SBus.Application.Features.Bookings.Queries.GetMyTickets.MyTicketDto>>("/api/v1/me/tickets");
        Assert.Contains(mine!.Data, item => item.Id == booking.BookingId);
        var theirs = await stranger.GetFromJsonAsync<PagedResponse<SBus.Application.Features.Bookings.Queries.GetMyTickets.MyTicketDto>>("/api/v1/me/tickets");
        Assert.DoesNotContain(theirs!.Data, item => item.Id == booking.BookingId);
    }

    private static HttpClient Client(WebAppFactory host) => host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task Login(WebAppFactory host, HttpClient client)
    {
        using var scope = host.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"booking-{Guid.NewGuid():N}@example.com";
        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Test Traveler", PhoneNumber = "+201012345678" };
        Assert.True((await users.CreateAsync(user, "test-password")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, Roles.Traveler)).Succeeded);
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "test-password"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
    }
}