using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameEventApi.Contracts;

namespace GameEventApi.Tests;

public class RegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Concurrent_registrations_never_exceed_capacity()
    {
        var ev = await CreateEventAsync(capacity: 30);

        // 50 players race for 30 seats at the same moment.
        var responses = await Task.WhenAll(Enumerable.Range(1, 50)
            .Select(i => RegisterAsync(ev.Id, $"Player {i}")));

        Assert.Equal(30, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        var rejected = responses.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        Assert.Equal(20, rejected.Count);
        Assert.All(rejected, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        foreach (var r in rejected)
        {
            Assert.Equal("event_full", await ProblemCodeAsync(r));
        }

        var after = await GetEventAsync(ev.Id);
        Assert.Equal(30, after.RegisteredCount);
        Assert.Equal(0, after.SeatsLeft);
    }

    [Fact]
    public async Task Concurrent_duplicate_names_register_once()
    {
        var ev = await CreateEventAsync(capacity: 30);

        var responses = await Task.WhenAll(Enumerable.Range(1, 10)
            .Select(_ => RegisterAsync(ev.Id, "Richard Garfield")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        foreach (var r in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
        {
            Assert.Equal("already_registered", await ProblemCodeAsync(r));
        }

        // Rolled-back duplicates must give their seats back.
        Assert.Equal(1, (await GetEventAsync(ev.Id)).RegisteredCount);
    }

    [Fact]
    public async Task Duplicate_name_ignores_case_and_spacing_and_keeps_the_seat()
    {
        var ev = await CreateEventAsync(capacity: 8);

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(ev.Id, "Alice Smith")).StatusCode);
        var dup = await RegisterAsync(ev.Id, "  alice   SMITH ");

        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        Assert.Equal("already_registered", await ProblemCodeAsync(dup));
        Assert.Equal(1, (await GetEventAsync(ev.Id)).RegisteredCount);
    }

    [Fact]
    public async Task Unknown_event_returns_not_found()
    {
        var response = await RegisterAsync(Guid.NewGuid(), "Nobody");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<EventDto> CreateEventAsync(int capacity)
    {
        var response = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            "Test event", "mtg", "Draft", DateTimeOffset.UtcNow.AddDays(1), capacity));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    private async Task<EventDto> GetEventAsync(Guid id) =>
        (await _client.GetFromJsonAsync<EventDto>($"/api/events/{id}"))!;

    private Task<HttpResponseMessage> RegisterAsync(Guid eventId, string name) =>
        _client.PostAsJsonAsync($"/api/events/{eventId}/registrations", new RegisterRequest(name));

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
