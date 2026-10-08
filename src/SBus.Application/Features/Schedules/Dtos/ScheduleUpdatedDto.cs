namespace SBus.Application.Features.Schedules.Dtos;

public sealed record ScheduleUpdatedDto(int UpdatedTrips, int TripsKeptBecauseOfBookings);
