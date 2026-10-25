using System.ComponentModel.DataAnnotations;
using GameEventApi.Data;

namespace GameEventApi.Contracts;

public record RegisterRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name);

public record RegistrationDto(Guid Id, Guid EventId, string PlayerName, DateTimeOffset CreatedAt)
{
    public static RegistrationDto From(Registration r) => new(
        r.Id, r.EventId, r.PlayerName, new DateTimeOffset(DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc)));
}
