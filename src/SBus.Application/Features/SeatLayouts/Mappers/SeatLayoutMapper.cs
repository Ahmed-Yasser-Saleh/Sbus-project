using SBus.Application.Features.SeatLayouts.Dtos;
using SBus.Domain.Fleet;

namespace SBus.Application.Features.SeatLayouts.Mappers;

public static class SeatLayoutMapper
{
    public static SeatLayoutDto ToDto(this SeatLayout entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new SeatLayoutDto(entity.Id, entity.Name, entity.Capacity, entity.ToGrid());
    }

    public static List<SeatLayoutDto> ToDtos(this IEnumerable<SeatLayout> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
