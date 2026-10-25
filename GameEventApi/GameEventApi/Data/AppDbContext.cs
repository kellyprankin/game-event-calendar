using Microsoft.EntityFrameworkCore;

namespace GameEventApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GameTemplate> GameTemplates => Set<GameTemplate>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameTemplate>(t =>
        {
            t.Property(x => x.Id).HasMaxLength(32);
            t.Property(x => x.Name).HasMaxLength(100);
            t.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_GameTemplates_Capacity",
                    "\"MinPlayers\" >= 1 AND \"DefaultCapacity\" >= \"MinPlayers\" AND \"MaxCapacity\" >= \"DefaultCapacity\"");
                tb.HasCheckConstraint("CK_GameTemplates_Duration", "\"DefaultDurationMinutes\" > 0");
            });
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Format).HasMaxLength(100);
            e.HasIndex(x => x.StartsAtUtc);
            e.HasOne(x => x.GameTemplate)
                .WithMany()
                .HasForeignKey(x => x.GameTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Last line of defense: the database itself refuses an overfilled event.
            e.ToTable(tb => tb.HasCheckConstraint("CK_Events_Capacity",
                "\"Capacity\" > 0 AND \"RegisteredCount\" >= 0 AND \"RegisteredCount\" <= \"Capacity\""));
        });

        modelBuilder.Entity<Registration>(r =>
        {
            r.Property(x => x.PlayerName).HasMaxLength(100);
            r.Property(x => x.NormalizedName).HasMaxLength(100);
            r.HasIndex(x => new { x.EventId, x.NormalizedName }).IsUnique();
            r.HasOne<Event>()
                .WithMany(x => x.Registrations)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
