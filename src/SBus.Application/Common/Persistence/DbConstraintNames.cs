namespace SBus.Application.Common.Persistence;

public static class DbConstraintNames
{
    public const string ActiveSeatPerTrip = "IX_BookingSeats_TripId_SeatNumber_Active";

    public const string TripPerScheduleAndDate = "IX_Trips_ScheduleId_ServiceDate";
}
