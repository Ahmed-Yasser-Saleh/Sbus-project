using SBus.Domain.Fleet;

namespace SBus.Tests.Common.Fleet;

public static class SeatLayoutFactory
{
    public const string MiniBusGrid = """
        D _ 1 2
        3 4 _ 5
        6 7 _ 8
        9 10 _ 11
        12 13 14
        """;

    public static SeatLayout MiniBus(string name = "Mini bus") =>
        SeatLayout.FromGrid(Guid.CreateVersion7(), name, MiniBusGrid).Value;
}
