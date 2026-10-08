using SBus.Domain.Fleet;
using SBus.Tests.Common.Fleet;

using Xunit;

namespace SBus.Domain.UnitTests.Fleet;

public class SeatLayoutTests
{
    [Fact]
    public void FromGrid_MiniBus_ParsesSeatsAndDriver()
    {
        var layout = SeatLayoutFactory.MiniBus();

        Assert.Equal(14, layout.Capacity);
        Assert.Equal(5, layout.Rows);
        Assert.Equal(4, layout.Columns);
        Assert.Equal(1, layout.DriverRow);
        Assert.Equal(1, layout.DriverColumn);

        var seat5 = layout.Seats.Single(s => s.SeatNumber == 5);
        Assert.Equal(2, seat5.Row);
        Assert.Equal(4, seat5.Column);
    }

    [Fact]
    public void ToGrid_RoundTripsTheOriginalGrid()
    {
        var layout = SeatLayoutFactory.MiniBus();

        var grid = layout.ToGrid();
        var reparsed = SeatLayout.FromGrid(Guid.CreateVersion7(), "copy", grid).Value;

        Assert.Equal(
            layout.Seats.Select(s => (s.SeatNumber, s.Row, s.Column)).OrderBy(s => s.SeatNumber),
            reparsed.Seats.Select(s => (s.SeatNumber, s.Row, s.Column)).OrderBy(s => s.SeatNumber));
    }

    [Fact]
    public void FromGrid_DuplicateSeatNumber_Fails()
    {
        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), "dup", "1 2\n2 3");

        Assert.Equal("SeatLayoutErrors.DuplicateSeat", result.TopError.Code);
    }

    [Fact]
    public void FromGrid_UnknownCell_Fails()
    {
        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), "bad", "1 X 2");

        Assert.Equal("SeatLayoutErrors.InvalidCell", result.TopError.Code);
    }

    [Fact]
    public void FromGrid_TwoDrivers_Fails()
    {
        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), "two", "D 1\nD 2");

        Assert.Equal(SeatLayoutErrors.MultipleDrivers.Code, result.TopError.Code);
    }

    [Fact]
    public void FromGrid_OnlyEmptyCells_Fails()
    {
        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), "empty", "_ _\nD _");

        Assert.Equal(SeatLayoutErrors.NoSeats.Code, result.TopError.Code);
    }

    [Fact]
    public void FromGrid_IgnoresBlankLinesAndWindowsLineEndings()
    {
        var result = SeatLayout.FromGrid(Guid.CreateVersion7(), "crlf", "1 2\r\n\r\n3 4\r\n");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Rows);
        Assert.Equal(4, result.Value.Capacity);
    }
}
