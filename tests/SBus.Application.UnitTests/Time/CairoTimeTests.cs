using SBus.Application.Common.Time;

using Xunit;

namespace SBus.Application.UnitTests.Time;

public class CairoTimeTests
{
    [Fact]
    public void ToUtc_Winter_IsTwoHoursBehind()
    {
        var utc = CairoTime.ToUtc(new DateOnly(2030, 1, 15), new TimeOnly(9, 0));

        Assert.Equal(new DateTimeOffset(2030, 1, 15, 7, 0, 0, TimeSpan.Zero), utc);
        Assert.Equal(TimeSpan.Zero, utc.Offset);
    }

    [Fact]
    public void ToUtc_Summer_IsThreeHoursBehind()
    {
        var utc = CairoTime.ToUtc(new DateOnly(2030, 7, 15), new TimeOnly(9, 0));

        Assert.Equal(new DateTimeOffset(2030, 7, 15, 6, 0, 0, TimeSpan.Zero), utc);
    }

    [Fact]
    public void Today_JustAfterLocalMidnight_IsTheNewLocalDay()
    {
        var nowUtc = new DateTimeOffset(2030, 1, 14, 22, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2030, 1, 15), CairoTime.Today(nowUtc));
    }
}
