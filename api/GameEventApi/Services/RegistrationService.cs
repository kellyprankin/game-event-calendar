using GameEventApi.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameEventApi.Services;

public abstract record RegistrationResult
{
    public sealed record Registered(Registration Registration) : RegistrationResult;
    public sealed record EventNotFound : RegistrationResult;
    public sealed record EventFull : RegistrationResult;
    public sealed record AlreadyRegistered : RegistrationResult;
}

public class RegistrationService(AppDbContext db, TimeProvider clock)
{
    public async Task<RegistrationResult> RegisterAsync(Guid eventId, string playerName, CancellationToken ct = default)
    {
        var displayName = CollapseWhitespace(playerName);
        var normalizedName = displayName.ToLowerInvariant();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Claim a seat with one conditional UPDATE. Postgres locks the event row for the
        // rest of the transaction, so concurrent requests for the last seat queue up and
        // only one of them sees RegisteredCount < Capacity. Never count-then-insert.
        var claimed = await db.Events
            .Where(e => e.Id == eventId && e.RegisteredCount < e.Capacity)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.RegisteredCount, e => e.RegisteredCount + 1), ct);

        if (claimed == 0)
        {
            if (!await db.Events.AnyAsync(e => e.Id == eventId, ct))
            {
                return new RegistrationResult.EventNotFound();
            }

            // Someone retrying after the event filled should hear they're already in, not "full".
            var alreadyIn = await db.Registrations.AnyAsync(
                r => r.EventId == eventId && r.NormalizedName == normalizedName, ct);
            return alreadyIn ? new RegistrationResult.AlreadyRegistered() : new RegistrationResult.EventFull();
        }

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            PlayerName = displayName,
            NormalizedName = normalizedName,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
        };
        db.Registrations.Add(registration);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duplicate name: undo the seat claim so the seat isn't lost.
            await tx.RollbackAsync(ct);
            return new RegistrationResult.AlreadyRegistered();
        }

        await tx.CommitAsync(ct);
        return new RegistrationResult.Registered(registration);
    }

    static string CollapseWhitespace(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
