using GameEventApi.Data;

namespace GameEventApi.Contracts;

public record TemplateDto(
    string Id,
    string Name,
    IReadOnlyList<string> Formats,
    int DefaultDurationMinutes,
    int DefaultCapacity,
    int MaxCapacity,
    int MinPlayers)
{
    public static TemplateDto From(GameTemplate t) => new(
        t.Id, t.Name, t.Formats, t.DefaultDurationMinutes, t.DefaultCapacity, t.MaxCapacity, t.MinPlayers);
}
