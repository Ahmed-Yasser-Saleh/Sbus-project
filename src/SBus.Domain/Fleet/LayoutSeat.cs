using SBus.Domain.Common;

namespace SBus.Domain.Fleet;

public sealed class LayoutSeat : Entity
{
    public Guid SeatLayoutId { get; private set; }
    public int SeatNumber { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }

    internal LayoutSeat(Guid id, Guid seatLayoutId, int seatNumber, int row, int column)
        : base(id)
    {
        SeatLayoutId = seatLayoutId;
        SeatNumber = seatNumber;
        Row = row;
        Column = column;
    }

    private LayoutSeat()
    { }
}
