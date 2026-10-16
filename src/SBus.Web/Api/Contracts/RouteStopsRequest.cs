using System.ComponentModel.DataAnnotations;
using SBus.Application.Features.Routes.Commands.SetRouteStops;

namespace SBus.Web.Api.Contracts;

public sealed record RouteStopsRequest([Required] List<RouteStopItem> Stops);

