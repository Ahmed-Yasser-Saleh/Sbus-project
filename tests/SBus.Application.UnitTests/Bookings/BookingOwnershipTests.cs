using SBus.Application.Common.Security;
using SBus.Tests.Common.Bookings;

using Xunit;

namespace SBus.Application.UnitTests.Bookings;

public class BookingOwnershipTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("another-traveler", false)]
    [InlineData("owner", true)]
    public void OwnedBooking_RequiresExactOwner(string? caller, bool allowed)
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.AssignTraveler("owner");
        Assert.Equal(allowed, BookingOwnership.AccessibleTo(caller).Compile()(booking));
    }

    [Fact]
    public void LegacyUnownedBooking_RetainsBearerLinkAccess()
    {
        Assert.True(BookingOwnership.AccessibleTo(null).Compile()(BookingFactory.CreateHold().Value));
    }

    [Fact]
    public void Ownership_CannotBeTransferred()
    {
        var booking = BookingFactory.CreateHold().Value;
        booking.AssignTraveler("owner");
        Assert.Throws<InvalidOperationException>(() => booking.AssignTraveler("another-traveler"));
    }
}

