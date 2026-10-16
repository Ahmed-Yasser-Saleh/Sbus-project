using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SBus.Application.Common.Errors;
using SBus.Application.Features.Buses.Commands.CreateBus;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Drivers.Commands.CreateDriver;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.MobileAuth;
using SBus.Application.Features.Routes.Commands.SetRouteStops;
using SBus.Application.Features.Routes.Dtos;
using SBus.Application.Features.Schedules.Commands.CreateSchedule;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.SubcutaneousTests.Common;
using SBus.Domain.Routes;
using SBus.Infrastructure.Identity;
using SBus.Web.Api.Contracts;
using Xunit;

namespace SBus.Application.SubcutaneousTests.Features.Settings;

[Collection(WebAppFactoryCollection.Name)]
public sealed class OfficeSettingsApiIntegrationTests(WebAppFactory database)
{
    [DatabaseFact]
    public async Task SettingsApi_PersistsCreatesUpdatesFiltersAndPagesWithoutSchemaChanges()
    {
        await using var host = new WebAppFactory { UseHttpUser = true };
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using (var scope = host.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(Roles.Office))
            {
                Assert.True((await roles.CreateAsync(new IdentityRole(Roles.Office))).Succeeded);
            }

            var email = $"settings-{Guid.NewGuid():N}@example.com";
            var user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, "test-password")).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, Roles.Office)).Succeeded);
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "test-password"));
            var session = (await login.Content.ReadFromJsonAsync<ApiResponse<SessionResponse>>())!.Data;
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        }

        var suffix = Guid.NewGuid().ToString("N");
        var stopRequest = new CreateStopCommand("API stop " + suffix, "Description");
        var stop = await Create<StopDto>("stops", stopRequest);
        var duplicate = await client.PostAsJsonAsync("/api/v1/office/stops", stopRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(ErrorCodes.StopNameExists, await duplicate.Content.ReadAsStringAsync());
        await Update("stops", stop.StopId, new UpdateStopRequest(stop.Name + " updated", null, false));
        var stops = await client.GetFromJsonAsync<PagedResponse<StopDto>>("/api/v1/office/stops?pageSize=100&activeOnly=true");
        Assert.DoesNotContain(stops!.Data, x => x.StopId == stop.StopId);
        var first = await client.GetFromJsonAsync<PagedResponse<StopDto>>("/api/v1/office/stops?page=1&pageSize=1");
        var second = await client.GetFromJsonAsync<PagedResponse<StopDto>>("/api/v1/office/stops?page=2&pageSize=1");
        Assert.True(first!.Pagination.HasMore);
        Assert.NotEqual(Assert.Single(first.Data).StopId, Assert.Single(second!.Data).StopId);
        var layout = await Create<SeatLayoutDto>("seat-layouts", new CreateSeatLayoutCommand("API layout " + suffix, "D 1 2"));
        Assert.Equal(2, layout.Capacity);
        var bus = await Create<Guid>("buses", new CreateBusCommand("API " + suffix, layout.SeatLayoutId));
        var driver = await Create<Guid>("drivers", new CreateDriverCommand("API driver " + suffix, "01012345678"));
        var schedule = await Create<Guid>("schedules", new CreateScheduleCommand(Direction.SuezToCairo, new TimeOnly(23, 17), 150, bus, driver));
        var schedules = await client.GetFromJsonAsync<PagedResponse<ScheduleDto>>("/api/v1/office/schedules?pageSize=100");
        Assert.Contains(schedules!.Data, x => x.ScheduleId == schedule);
        var updated = await client.PutAsJsonAsync($"/api/v1/office/schedules/{schedule}", new UpdateScheduleRequest(new(23, 18), 175, bus, driver, false));
        Assert.True(updated.IsSuccessStatusCode, await updated.Content.ReadAsStringAsync());
        Assert.NotNull(await updated.Content.ReadFromJsonAsync<ApiResponse<ScheduleUpdatedDto>>());
        await Update("buses", bus, new UpdateBusRequest("API " + suffix, layout.SeatLayoutId, false));
        await Update("drivers", driver, new UpdateDriverRequest("API driver " + suffix, "01012345678", false));
        var buses = await client.GetFromJsonAsync<PagedResponse<BusDto>>("/api/v1/office/buses?activeOnly=true&pageSize=100");
        Assert.DoesNotContain(buses!.Data, x => x.BusId == bus);
        var drivers = await client.GetFromJsonAsync<PagedResponse<DriverDto>>("/api/v1/office/drivers?activeOnly=true&pageSize=100");
        Assert.DoesNotContain(drivers!.Data, x => x.DriverId == driver);
        var layouts = await client.GetFromJsonAsync<PagedResponse<SeatLayoutDto>>("/api/v1/office/seat-layouts?pageSize=100");
        Assert.Contains(layouts!.Data, x => x.SeatLayoutId == layout.SeatLayoutId);
        var route = new RouteStopsRequest([new(database.World.FirstStopId, 0), new(database.World.MiddleStopId, 30), new(database.World.LastStopId, 120)]);
        var routeResponse = await client.PutAsJsonAsync("/api/v1/office/routes/CairoToSuez", route);
        Assert.True(routeResponse.IsSuccessStatusCode, await routeResponse.Content.ReadAsStringAsync());
        var routeRead = (await client.GetFromJsonAsync<ApiResponse<List<RouteStopDto>>>("/api/v1/office/routes/CairoToSuez"))!.Data;
        Assert.Equal(route.Stops.Select(x => x.StopId), routeRead.Select(x => x.StopId));
        Assert.Equal(new[] { 0, 30, 120 }, routeRead.Select(x => x.MinutesFromStart));
        var inactive = await client.PutAsJsonAsync("/api/v1/office/routes/CairoToSuez", new RouteStopsRequest([new(database.World.FirstStopId, 0), new(stop.StopId, 120)]));
        Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
        Assert.Contains(ErrorCodes.StopsNotFoundOrInactive, await inactive.Content.ReadAsStringAsync());

        async Task<T> Create<T>(string resource, object request)
        {
            var response = await client.PostAsJsonAsync("/api/v1/office/" + resource, request);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<ApiResponse<T>>())!.Data;
        }

        async Task Update(string resource, Guid id, object request)
        {
            var response = await client.PutAsJsonAsync($"/api/v1/office/{resource}/{id}", request);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}

