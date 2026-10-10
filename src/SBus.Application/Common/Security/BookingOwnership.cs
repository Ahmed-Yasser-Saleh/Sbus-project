using System.Linq.Expressions;

using SBus.Domain.Bookings;

namespace SBus.Application.Common.Security;

public static class BookingOwnership
{
    // Only unowned legacy bookings retain the original bearer-link behavior.
    public static Expression<Func<Booking, bool>> AccessibleTo(string? userId) =>
        booking => booking.UserId == null || (userId != null && booking.UserId == userId);
}
