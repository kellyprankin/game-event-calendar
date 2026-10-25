using System.ComponentModel.DataAnnotations;
using GameEventApi.Data;

namespace GameEventApi.Contracts;

/// <summary>
/// Capacity is optional and defaults to the template's value. Duration always
/// comes from the template.
/// </summary>
public record CreateEventRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Required] string GameTemplateId,
    [property: Required] string Format,
    DateTimeOffset StartsAt,
    [property: Range(1, 1000)] int? Capacity);

public record EventDto(
    Guid Id,
    string Name,
    string GameTemplateId,
    string GameName,
    string Format,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int DurationMinutes,
    int Capacity,
    int RegisteredCount,
    int SeatsLeft,
    int MinPlayers)
{
    public static EventDto From(Event e, GameTemplate t)
    {
        var startsAt = new DateTimeOffset(DateTime.SpecifyKind(e.StartsAtUtc, DateTimeKind.Utc));
        return new(
            e.Id, e.Name, e.GameTemplateId, t.Name, e.Format,
            startsAt, startsAt.AddMinutes(e.DurationMinutes), e.DurationMinutes,
            e.Capacity, e.RegisteredCount, e.Capacity - e.RegisteredCount, e.MinPlayers);
    }
}
