namespace GameEventApi.Data;

public class Registration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string PlayerName { get; set; }

    // Trimmed, whitespace-collapsed, lowercased name; unique per event to catch duplicates.
    public required string NormalizedName { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
