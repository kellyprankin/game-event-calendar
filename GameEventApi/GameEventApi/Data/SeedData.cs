using Microsoft.EntityFrameworkCore;

namespace GameEventApi.Data;

/// <summary>
/// Game templates inserted on startup. Adding a game means adding an entry here
/// (or a row in the table); no event logic changes.
/// </summary>
public static class SeedData
{
    public static GameTemplate[] Templates =>
    [
        new()
        {
            Id = "mtg",
            Name = "Magic: The Gathering",
            Formats = ["Standard", "Pioneer", "Modern", "Commander", "Draft"],
            DefaultDurationMinutes = 240,
            DefaultCapacity = 16,
            MaxCapacity = 30,
            MinPlayers = 8,
        },
        new()
        {
            Id = "vtes",
            Name = "Vampire: The Eternal Struggle",
            Formats = ["Constructed", "Limited"],
            DefaultDurationMinutes = 300,
            DefaultCapacity = 15,
            MaxCapacity = 30,
            MinPlayers = 4,
        },
        new()
        {
            Id = "netrunner",
            Name = "Netrunner",
            Formats = ["Standard", "Startup", "Eternal"],
            DefaultDurationMinutes = 240,
            DefaultCapacity = 12,
            MaxCapacity = 24,
            MinPlayers = 4,
        },
        // Not a card game: shows the template system isn't card-specific.
        new()
        {
            Id = "dnd",
            Name = "Dungeons & Dragons",
            Formats = ["One-shot", "Campaign session", "Intro adventure"],
            DefaultDurationMinutes = 240,
            DefaultCapacity = 6,
            MaxCapacity = 7,
            MinPlayers = 3,
        },
    ];

    // Inserts missing templates only, so edits made to existing rows are kept.
    public static async Task SeedTemplatesAsync(DbContext db, CancellationToken ct = default)
    {
        var templates = db.Set<GameTemplate>();
        var existing = await templates.Select(t => t.Id).ToListAsync(ct);
        templates.AddRange(Templates.Where(t => !existing.Contains(t.Id)));
        await db.SaveChangesAsync(ct);
    }

    public static void SeedTemplates(DbContext db)
    {
        var templates = db.Set<GameTemplate>();
        var existing = templates.Select(t => t.Id).ToList();
        templates.AddRange(Templates.Where(t => !existing.Contains(t.Id)));
        db.SaveChanges();
    }
}
