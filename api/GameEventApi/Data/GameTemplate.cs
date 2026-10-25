namespace GameEventApi.Data;

/// <summary>
/// Describes a game the store runs events for. Event rules come from these values,
/// so supporting a new game means adding a row, not changing code.
/// </summary>
public class GameTemplate
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public List<string> Formats { get; set; } = [];
    public int DefaultDurationMinutes { get; set; }
    public int DefaultCapacity { get; set; }
    public int MaxCapacity { get; set; }
    public int MinPlayers { get; set; }

    // Position in pickers and lists; lower comes first.
    public int DisplayOrder { get; set; }
}
