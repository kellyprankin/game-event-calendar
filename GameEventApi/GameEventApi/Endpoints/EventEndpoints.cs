using GameEventApi.Contracts;
using System.Text;
using GameEventApi.Data;
using GameEventApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameEventApi.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder api)
    {
        var events = api.MapGroup("/events").WithTags("Events");

        events.MapGet("/", ListEvents);
        events.MapGet("/{id:guid}", GetEvent);
        events.MapPost("/", CreateEvent);
        events.MapGet("/{id:guid}/invite.ics", GetInvite);

        return api;
    }

    static async Task<Results<FileContentHttpResult, NotFound>> GetInvite(
        Guid id, AppDbContext db, IOptions<StoreOptions> store, TimeProvider clock, CancellationToken ct)
    {
        var ev = await db.Events.AsNoTracking().Include(e => e.GameTemplate)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null)
        {
            return TypedResults.NotFound();
        }

        var ics = CalendarInvite.Create(ev, ev.GameTemplate!, store.Value, clock.GetUtcNow().UtcDateTime);
        return TypedResults.File(Encoding.UTF8.GetBytes(ics), "text/calendar", $"{FileName(ev.Name)}.ics");
    }

    static string FileName(string name)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(safe) ? "event" : safe;
    }

    // Both bounds are optional so the calendar can ask for a day, a month, or everything.
    static async Task<Ok<List<EventDto>>> ListEvents(
        DateTimeOffset? from, DateTimeOffset? to, AppDbContext db, CancellationToken ct)
    {
        var query = db.Events.AsNoTracking().Include(e => e.GameTemplate).AsQueryable();
        if (from is { } f) query = query.Where(e => e.StartsAtUtc >= f.UtcDateTime);
        if (to is { } t) query = query.Where(e => e.StartsAtUtc < t.UtcDateTime);

        var events = await query.OrderBy(e => e.StartsAtUtc).ToListAsync(ct);
        return TypedResults.Ok(events.Select(e => EventDto.From(e, e.GameTemplate!)).ToList());
    }

    static async Task<Results<Ok<EventDto>, NotFound>> GetEvent(Guid id, AppDbContext db, CancellationToken ct)
    {
        var ev = await db.Events.AsNoTracking().Include(e => e.GameTemplate)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return ev is null ? TypedResults.NotFound() : TypedResults.Ok(EventDto.From(ev, ev.GameTemplate!));
    }

    static async Task<Results<Created<EventDto>, ValidationProblem>> CreateEvent(
        CreateEventRequest request, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var template = await db.GameTemplates.FindAsync([request.GameTemplateId], ct);
        if (template is null)
        {
            return Invalid(nameof(request.GameTemplateId), $"Unknown game '{request.GameTemplateId}'.");
        }

        // Every rule below comes from the template's data; nothing here knows which game it is.
        if (!template.Formats.Contains(request.Format))
        {
            return Invalid(nameof(request.Format),
                $"'{request.Format}' is not a {template.Name} format. Choose one of: {string.Join(", ", template.Formats)}.");
        }

        var capacity = request.Capacity ?? template.DefaultCapacity;
        if (capacity < template.MinPlayers || capacity > template.MaxCapacity)
        {
            return Invalid(nameof(request.Capacity),
                $"Capacity for {template.Name} must be between {template.MinPlayers} and {template.MaxCapacity}.");
        }

        var now = clock.GetUtcNow();
        if (request.StartsAt <= now)
        {
            return Invalid(nameof(request.StartsAt), "Start time must be in the future.");
        }

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            GameTemplateId = template.Id,
            Format = request.Format,
            StartsAtUtc = request.StartsAt.UtcDateTime,
            DurationMinutes = template.DefaultDurationMinutes,
            Capacity = capacity,
            MinPlayers = template.MinPlayers,
            CreatedAtUtc = now.UtcDateTime,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/events/{ev.Id}", EventDto.From(ev, template));
    }

    static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
