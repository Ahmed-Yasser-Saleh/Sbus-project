using SBus.Domain.Routes;

using Xunit;

namespace SBus.Domain.UnitTests.Routes;

public class RouteStopTests
{
    private static readonly Guid A = Guid.CreateVersion7();
    private static readonly Guid B = Guid.CreateVersion7();
    private static readonly Guid C = Guid.CreateVersion7();

    [Fact]
    public void CreateSequence_Valid_AssignsOrderFromPosition()
    {
        var result = RouteStop.CreateSequence(Direction.CairoToSuez, [(A, 0), (B, 20), (C, 120)]);

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2, 3], result.Value.Select(r => r.Order));
        Assert.Equal([A, B, C], result.Value.Select(r => r.StopId));
    }

    [Fact]
    public void CreateSequence_SingleStop_Fails()
    {
        var result = RouteStop.CreateSequence(Direction.CairoToSuez, [(A, 0)]);

        Assert.Equal(RouteStopErrors.AtLeastTwoStops.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateSequence_DuplicateStop_Fails()
    {
        var result = RouteStop.CreateSequence(Direction.CairoToSuez, [(A, 0), (B, 10), (A, 20)]);

        Assert.Equal(RouteStopErrors.DuplicateStop.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateSequence_FirstStopNotAtZero_Fails()
    {
        var result = RouteStop.CreateSequence(Direction.CairoToSuez, [(A, 5), (B, 10)]);

        Assert.Equal(RouteStopErrors.FirstStopMustStartAtZero.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateSequence_MinutesNotIncreasing_Fails()
    {
        var result = RouteStop.CreateSequence(Direction.CairoToSuez, [(A, 0), (B, 30), (C, 30)]);

        Assert.Equal(RouteStopErrors.MinutesMustIncrease.Code, result.TopError.Code);
    }
}
