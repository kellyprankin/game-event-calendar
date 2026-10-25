using GameEventApi.Contracts;
using GameEventApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GameEventApi.Endpoints;

public static class RegistrationEndpoints
{
    public static RouteGroupBuilder MapRegistrationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGroup("/events/{eventId:guid}/registrations").WithTags("Registrations")
            .MapPost("/", Register);

        return api;
    }

    static async Task<Results<Created<RegistrationDto>, NotFound, Conflict<ProblemDetails>>> Register(
        Guid eventId, RegisterRequest request, RegistrationService registrations, CancellationToken ct)
    {
        return await registrations.RegisterAsync(eventId, request.Name, ct) switch
        {
            RegistrationResult.Registered r => TypedResults.Created(
                $"/api/events/{eventId}/registrations/{r.Registration.Id}", RegistrationDto.From(r.Registration)),
            RegistrationResult.EventNotFound => TypedResults.NotFound(),
            RegistrationResult.EventFull => Conflict("event_full", "This event is full.",
                "All seats have been taken, so no more registrations can be accepted."),
            RegistrationResult.AlreadyRegistered => Conflict("already_registered", "You're already registered.",
                "Someone with this name is already registered for this event."),
            _ => throw new InvalidOperationException("Unhandled registration result."),
        };
    }

    // `code` gives the frontend a stable value to switch on instead of parsing the title.
    static Conflict<ProblemDetails> Conflict(string code, string title, string detail) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = title,
            Detail = detail,
            Extensions = { ["code"] = code },
        });
}
