using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SBus.Application.Common.Time;
using SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.MobileAuth;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.SubcutaneousTests.Common;
using SBus.Domain.Bookings;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Contracts;
using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Bookings;

[Collection(WebAppFactoryCollection.Name)]
public sealed class OfficeApiIntegrationTests(WebAppFactory database)
{
    [DatabaseFact]
    public async Task OfficeApi_CreatesConfirmedBookingsAndReviewsRealReceipts()
    {
        await using var host = new WebAppFactory { UseHttpUser = true };
        using var office = Client(host);
        using var traveler = Client(host);
        await Login(office, Roles.Office);
        await Login(traveler, Roles.Traveler);
        await using var db = database.CreateDbContext();
        var trip = await database.World.CreateTripAsync(db, database.Clock.GetUtcNow().AddDays(2));
        var officeBooking = await office.PostAsJsonAsync($"/api/v1/office/trips/{trip}/bookings", new OfficeBookingRequest("Walk in", "01012345678", database.World.FirstStopId, database.World.LastStopId, [1]));
        Assert.True(officeBooking.IsSuccessStatusCode, await officeBooking.Content.ReadAsStringAsync());
        var walkIn = (await officeBooking.Content.ReadFromJsonAsync<ApiResponse<BookingCreatedDto>>())!.Data;
        var manifest = (await office.GetFromJsonAsync<ApiResponse<TripManifestDto>>($"/api/v1/office/trips/{trip}/manifest"))!.Data;
        Assert.Equal(BookingStatus.Confirmed, Assert.Single(manifest.Bookings).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await office.PostAsync($"/api/v1/office/bookings/{walkIn.BookingId}/payment/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await office.PutAsJsonAsync($"/api/v1/office/trips/{trip}/assignment", new TripAssignmentRequest(database.World.BusId, database.World.DriverId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await office.PostAsync($"/api/v1/office/bookings/{walkIn.BookingId}/cancel", null)).StatusCode);
        var online = await traveler.PostAsJsonAsync("/api/v1/bookings", new CreateOnlineBookingCommand(trip, database.World.FirstStopId, database.World.LastStopId, [2]));
        Assert.True(online.IsSuccessStatusCode, await online.Content.ReadAsStringAsync());
        var pending = (await online.Content.ReadFromJsonAsync<ApiResponse<BookingCreatedDto>>())!.Data;
        using var image = new MultipartFormDataContent();
        byte[] png = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0x0d];
        image.Add(new ByteArrayContent(png), "receipt", "receipt.png");
        Assert.Equal(HttpStatusCode.OK, (await traveler.PostAsync($"/api/v1/tickets/{pending.PublicToken}/receipt", image)).StatusCode);
        var payments = await office.GetFromJsonAsync<PagedResponse<PendingPaymentDto>>("/api/v1/office/payments?pageSize=100");
        Assert.Contains(payments!.Data, x => x.BookingId == pending.BookingId);
        var receipt = await office.GetAsync($"/api/v1/office/bookings/{pending.BookingId}/receipt");
        Assert.Equal(png, await receipt.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await traveler.GetAsync($"/api/v1/office/bookings/{pending.BookingId}/receipt")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await office.PostAsJsonAsync($"/api/v1/office/bookings/{pending.BookingId}/payment/reject", new RejectPaymentRequest("Incorrect amount"))).StatusCode);
        var ticket = (await traveler.GetFromJsonAsync<ApiResponse<TicketDto>>($"/api/v1/tickets/{pending.PublicToken}"))!.Data;
        Assert.Equal(BookingStatus.Rejected, ticket.Status);
        Assert.Equal("Incorrect amount", ticket.RejectionReason);
        var second = await traveler.PostAsJsonAsync("/api/v1/bookings", new CreateOnlineBookingCommand(trip, database.World.FirstStopId, database.World.LastStopId, [3]));
        var toConfirm = (await second.Content.ReadFromJsonAsync<ApiResponse<BookingCreatedDto>>())!.Data;
        using var secondImage = new MultipartFormDataContent();
        secondImage.Add(new ByteArrayContent(png), "receipt", "receipt.png");
        Assert.Equal(HttpStatusCode.OK, (await traveler.PostAsync($"/api/v1/tickets/{toConfirm.PublicToken}/receipt", secondImage)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await office.PostAsync($"/api/v1/office/bookings/{toConfirm.BookingId}/payment/confirm", null)).StatusCode);
        var confirmed = (await traveler.GetFromJsonAsync<ApiResponse<TicketDto>>($"/api/v1/tickets/{toConfirm.PublicToken}"))!.Data;
        Assert.Equal(BookingStatus.Confirmed, confirmed.Status);
        Assert.NotNull(confirmed.DriverName);
        var day = await office.GetFromJsonAsync<ApiResponse<OfficeDayResponse>>("/api/v1/office/day?date=" + manifest.ServiceDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(day!.Data.Trips, x => x.TripId == trip);

        async Task Login(HttpClient client, string role)
        {
            using var scope = host.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(role))
            {
                Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
            }

            var email = $"office-api-{Guid.NewGuid():N}@example.com";
            var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "API Test", PhoneNumber = "01012345678" };
            Assert.True((await users.CreateAsync(user, "test-password")).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "test-password"));
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var session = (await response.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        }
    }

    private static HttpClient Client(WebAppFactory host) => host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
}

