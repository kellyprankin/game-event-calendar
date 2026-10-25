using GameEventApi.Contracts;
using GameEventApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GameEventApi.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder api)
    {
        var events = api.MapGroup("/events").WithTags("Events");

        events.MapPost("/", CreateEvent);

        return api;
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
