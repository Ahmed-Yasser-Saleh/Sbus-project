using SBus.Application.Features.Stops.Dtos;
using SBus.Domain.Stops;

namespace SBus.Application.Features.Stops.Mappers;

public static class StopMapper
{
    public static StopDto ToDto(this Stop entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new StopDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public static List<StopDto> ToDtos(this IEnumerable<Stop> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
