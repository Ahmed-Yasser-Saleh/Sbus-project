using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MediatR;

using NSubstitute;
using NSubstitute.Extensions;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Application.Common.Models;
using SBus.Application.Features.Buses.Commands.CreateBus;
using SBus.Application.Features.Buses.Commands.UpdateBus;
using SBus.Application.Features.Buses.Dtos;
using SBus.Application.Features.Buses.Queries.GetBuses;
using SBus.Application.Features.Drivers.Commands.CreateDriver;
using SBus.Application.Features.Drivers.Commands.UpdateDriver;
using SBus.Application.Features.Drivers.Dtos;
using SBus.Application.Features.Drivers.Queries.GetDrivers;
using SBus.Application.Features.Routes.Commands.SetRouteStops;
using SBus.Application.Features.Routes.Dtos;
using SBus.Application.Features.Routes.Queries.GetRoute;
using SBus.Application.Features.Schedules.Commands.CreateSchedule;
using SBus.Application.Features.Schedules.Commands.UpdateSchedule;
using SBus.Application.Features.Schedules.Dtos;
using SBus.Application.Features.Schedules.Queries.GetSchedules;
using SBus.Application.Features.SeatLayouts.Commands.CreateSeatLayout;
using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Application.Features.SeatLayouts.Queries.GetSeatLayouts;
using SBus.Application.Features.Stops.Commands.CreateStop;
using SBus.Application.Features.Stops.Commands.UpdateStop;
using SBus.Application.Features.Stops.Dtos;
using SBus.Application.Features.Stops.Queries.GetStops;
using SBus.Domain.Common.Results;
using SBus.Domain.Routes;
using SBus.Web.Api.Contracts;

using Xunit;

namespace SBus.Api.Tests;

public sealed class OfficeSettingsEndpointTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    public static IEnumerable<object[]> Endpoints()
    {
        foreach (var resource in new[] { "stops", "routes", "seat-layouts", "buses", "drivers", "schedules" })
        {
            yield return [resource, "GET"];
            yield return [resource, resource == "routes" ? "PUT" : "POST"];
            if (resource is "stops" or "buses" or "drivers" or "schedules")
            {
                yield return [resource, "PUT"];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task SettingsSuccess_UsesSharedQueriesAndCommands(string resource, string method)
    {
        await using var factory = new ApiTestFactory();
        SetResults(factory, null);
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var response = await Send(client, resource, method);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.True(json.TryGetProperty("data", out _));
        Assert.True(response.Headers.CacheControl!.NoStore);
        if (method == "GET" && resource != "routes")
        {
            Assert.Equal(2, json.GetProperty("pagination").GetProperty("page").GetInt32());
            Assert.Equal(1, json.GetProperty("pagination").GetProperty("pageSize").GetInt32());
            Assert.True(json.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
            Assert.Equal(1, json.GetProperty("data").GetArrayLength());
        }

        var calls = factory.Sender.ReceivedCalls().Select(call => call.GetArguments()[0]).ToArray();
        var request = Assert.Single(calls);
        if (request is SetRouteStopsCommand route)
        {
            Assert.Equal(Direction.CairoToSuez, route.Direction);
            Assert.Equal(new[] { new RouteStopItem(Id, 0), new RouteStopItem(Other, 120) }, route.Stops);
        }
        else
        {
            Assert.Equal(Expected(resource, method), request);
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EverySettingsEndpoint_RejectsUnauthenticatedTravelerAndCompany(string resource, string method)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        await AccountEndpointTests.Problem(await Send(client, resource, method), 401, ErrorCodes.Unauthenticated);
        foreach (var roles in new[] { new[] { "Traveler" }, new[] { "CompanyOwner" }, new[] { "CompanyEmployee" }, new[] { "Office", "CompanyEmployee" } })
        {
            factory.Authenticate(client, roles);
            await AccountEndpointTests.Problem(await Send(client, resource, method), 403, ErrorCodes.Forbidden);
        }
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EverySettingsEndpoint_ValidatesInputBeforeDispatch(string resource, string method)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var problem = await AccountEndpointTests.Problem(await Send(client, resource, method, true), 400, ErrorCodes.ValidationFailed);
        Assert.NotEmpty(problem.GetProperty("errors").EnumerateObject());
        Assert.Empty(factory.Sender.ReceivedCalls());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task EverySettingsEndpoint_MapsUnexpectedAndUnavailableFailures(string resource, string method)
    {
        await using var factory = new ApiTestFactory();
        SetResults(factory, new InvalidOperationException("private SQL"));
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var problem = await AccountEndpointTests.Problem(await Send(client, resource, method), 500, ErrorCodes.InternalError);
        Assert.DoesNotContain("private SQL", problem.ToString());
        SetResults(factory, new ServiceUnavailableException("Temporarily unavailable."));
        await AccountEndpointTests.Problem(await Send(client, resource, method), 503, ErrorCodes.ApiUnavailable);
    }

    [Theory]
    [InlineData("stops", "POST", "StopNameExists", 409, ErrorCodes.StopNameExists)]
    [InlineData("stops", "PUT", "StopNotFound", 404, ErrorCodes.StopNotFound)]
    [InlineData("stops", "PUT", "StopNameExists", 409, ErrorCodes.StopNameExists)]
    [InlineData("routes", "PUT", "StopsNotFoundOrInactive", 400, ErrorCodes.StopsNotFoundOrInactive)]
    [InlineData("seat-layouts", "POST", "SeatLayoutNameExists", 409, ErrorCodes.SeatLayoutNameExists)]
    [InlineData("buses", "POST", "SeatLayoutNotFound", 404, ErrorCodes.SeatLayoutNotFound)]
    [InlineData("buses", "POST", "PlateNumberExists", 409, ErrorCodes.PlateNumberExists)]
    [InlineData("buses", "PUT", "BusNotFound", 404, ErrorCodes.BusNotFound)]
    [InlineData("buses", "PUT", "SeatLayoutNotFound", 404, ErrorCodes.SeatLayoutNotFound)]
    [InlineData("buses", "PUT", "PlateNumberExists", 409, ErrorCodes.PlateNumberExists)]
    [InlineData("buses", "PUT", "LayoutDoesNotCoverBookedSeats", 409, ErrorCodes.LayoutDoesNotCoverBookedSeats)]
    [InlineData("drivers", "PUT", "DriverNotFound", 404, ErrorCodes.DriverNotFound)]
    [InlineData("schedules", "POST", "BusNotFound", 404, ErrorCodes.BusNotFound)]
    [InlineData("schedules", "POST", "DriverNotFound", 404, ErrorCodes.DriverNotFound)]
    [InlineData("schedules", "POST", "BusInactive", 409, ErrorCodes.BusInactive)]
    [InlineData("schedules", "POST", "DriverInactive", 409, ErrorCodes.DriverInactive)]
    [InlineData("schedules", "POST", "ScheduleExists", 409, ErrorCodes.ScheduleExists)]
    [InlineData("schedules", "PUT", "ScheduleNotFound", 404, ErrorCodes.ScheduleNotFound)]
    [InlineData("schedules", "PUT", "BusNotFound", 404, ErrorCodes.BusNotFound)]
    [InlineData("schedules", "PUT", "DriverNotFound", 404, ErrorCodes.DriverNotFound)]
    [InlineData("schedules", "PUT", "BusInactive", 409, ErrorCodes.BusInactive)]
    [InlineData("schedules", "PUT", "DriverInactive", 409, ErrorCodes.DriverInactive)]
    [InlineData("schedules", "PUT", "ScheduleExists", 409, ErrorCodes.ScheduleExists)]
    public async Task BusinessFailures_HaveSpecificStatusAndCode(string resource, string method, string errorName, int status, string code)
    {
        await using var factory = new ApiTestFactory();
        var error = errorName == nameof(ApplicationErrors.LayoutDoesNotCoverBookedSeats)
            ? ApplicationErrors.LayoutDoesNotCoverBookedSeats([1])
            : (Error)typeof(ApplicationErrors).GetProperty(errorName)!.GetValue(null)!;
        Configure<StopDto>(factory, error);
        Configure<SeatLayoutDto>(factory, error);
        Configure<Guid>(factory, error);
        Configure<Updated>(factory, error);
        Configure<ScheduleUpdatedDto>(factory, error);
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        await AccountEndpointTests.Problem(await Send(client, resource, method), status, code);
    }

    [Theory]
    [InlineData("{\"stops\":null}", "stops")]
    [InlineData("{\"stops\":[null]}", "stops[0]")]
    public async Task Route_NullCollectionsAndItemsReturnValidation(string json, string field)
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.Client();
        factory.Authenticate(client, "Office");
        var response = await client.PutAsync("/api/v1/office/routes/CairoToSuez", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        var problem = await AccountEndpointTests.Problem(response, 400, ErrorCodes.ValidationFailed);
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Empty(factory.Sender.ReceivedCalls());
    }

    private static object Expected(string resource, string method) => (resource, method) switch
    {
        ("stops", "GET") => new GetStopsQuery(true, new QueryPage(2, 1)),
        ("buses", "GET") => new GetBusesQuery(true, new QueryPage(2, 1)),
        ("drivers", "GET") => new GetDriversQuery(true, new QueryPage(2, 1)),
        ("seat-layouts", "GET") => new GetSeatLayoutsQuery(new QueryPage(2, 1)),
        ("schedules", "GET") => new GetSchedulesQuery(new QueryPage(2, 1)),
        ("routes", "GET") => new GetRouteQuery(Direction.CairoToSuez),
        ("stops", "POST") => new CreateStopCommand("Stop", "Description"),
        ("stops", "PUT") => new UpdateStopCommand(Id, "Stop", "Description", false),
        ("buses", "POST") => new CreateBusCommand("BUS 123", Other),
        ("buses", "PUT") => new UpdateBusCommand(Id, "BUS 123", Other, false),
        ("drivers", "POST") => new CreateDriverCommand("Driver", "01012345678"),
        ("drivers", "PUT") => new UpdateDriverCommand(Id, "Driver", "01012345678", false),
        ("seat-layouts", "POST") => new CreateSeatLayoutCommand("Layout", "D 1 2"),
        ("schedules", "POST") => new CreateScheduleCommand(Direction.CairoToSuez, new TimeOnly(9, 0), 150, Other, Other),
        ("schedules", "PUT") => new UpdateScheduleCommand(Id, new TimeOnly(9, 0), 150, Other, Other, false),

        // The route command contains a deserialized list; compare its elements explicitly in Send.
        _ => throw new InvalidOperationException(),
    };

    private static async Task<HttpResponseMessage> Send(HttpClient client, string resource, string method, bool invalid = false)
    {
        var path = "/api/v1/office/" + resource;
        if (resource == "routes")
        {
            path += invalid ? "/999" : "/CairoToSuez";
        }
        else if (method == "PUT")
        {
            path += "/" + Id;
        }

        if (method == "GET")
        {
            return await client.GetAsync(path + (invalid ? "?page=0" : "?page=2&pageSize=1&activeOnly=true"));
        }

        object body = (resource, method) switch
        {
            ("stops", _) => new { name = invalid ? string.Empty : "Stop", description = "Description", isActive = false },
            ("buses", _) => new { plateNumber = invalid ? string.Empty : "BUS 123", seatLayoutId = Other, isActive = false },
            ("drivers", _) => new { name = invalid ? string.Empty : "Driver", phoneNumber = "01012345678", isActive = false },
            ("seat-layouts", _) => new { name = invalid ? string.Empty : "Layout", grid = "D 1 2" },
            ("schedules", _) => new { direction = Direction.CairoToSuez, departureTime = "09:00:00", price = invalid ? 0 : 150, busId = Other, driverId = Other, isActive = false },
            _ => new { stops = new[] { new RouteStopItem(Id, 0), new RouteStopItem(Other, 120) } },
        };
        return method == "POST" ? await client.PostAsJsonAsync(path, body) : await client.PutAsJsonAsync(path, body);
    }

    private static void SetResults(ApiTestFactory factory, Exception? exception)
    {
        Configure<StopDto>(factory, new StopDto(Id, "Stop", null, true), exception);
        Configure<SeatLayoutDto>(factory, new SeatLayoutDto(Id, "Layout", 2, "D 1 2"), exception);
        Configure<Guid>(factory, Id, exception);
        Configure<Updated>(factory, Result.Updated, exception);
        Configure<ScheduleUpdatedDto>(factory, new ScheduleUpdatedDto(2, 1), exception);
        Configure<List<StopDto>>(factory, new List<StopDto> { new(Id, "A", null, true), new(Other, "B", null, true) }, exception);
        Configure<List<BusDto>>(factory, new List<BusDto> { new(Id, "A", Other, "L", 2, true), new(Other, "B", Other, "L", 2, true) }, exception);
        Configure<List<DriverDto>>(factory, new List<DriverDto> { new(Id, "A", "01012345678", true), new(Other, "B", "01012345678", true) }, exception);
        Configure<List<SeatLayoutDto>>(factory, new List<SeatLayoutDto> { new(Id, "A", 2, "1 2"), new(Other, "B", 2, "1 2") }, exception);
        Configure<List<ScheduleDto>>(factory, new List<ScheduleDto> { new(Id, Direction.CairoToSuez, new(9, 0), 150, Other, "A", Other, "D", true), new(Other, Direction.CairoToSuez, new(10, 0), 150, Other, "A", Other, "D", true) }, exception);
        Configure<List<RouteStopDto>>(factory, new List<RouteStopDto> { new(Id, "A", 0, 0), new(Other, "B", 1, 120) }, exception);
    }

    private static void Configure<T>(ApiTestFactory factory, Result<T> result, Exception? exception = null) =>
        factory.Sender.Configure().Send(Arg.Any<IRequest<Result<T>>>(), Arg.Any<CancellationToken>()).Returns(_ => exception is null ? result : throw exception);
}