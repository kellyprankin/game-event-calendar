namespace GameEventApi.Data;

public class Event
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string GameTemplateId { get; set; }
    public GameTemplate? GameTemplate { get; set; }
    public required string Format { get; set; }
    public DateTime StartsAtUtc { get; set; }

    // Copied from the template when the event is created, so later template
    // changes don't alter events that are already scheduled.
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int MinPlayers { get; set; }

    // Only changed by the atomic seat claim in RegistrationService.
    public int RegisteredCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public List<Registration> Registrations { get; set; } = [];
}
