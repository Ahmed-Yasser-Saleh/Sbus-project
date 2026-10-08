using SBus.Application.Features.Trips.Dtos;

namespace SBus.Web.Pages.Shared;

public sealed record SeatMapView(
    SeatMapDto Map,
    bool Selectable,
    IReadOnlyCollection<int> Selected,
    string InputName = "Input.SeatNumbers");
