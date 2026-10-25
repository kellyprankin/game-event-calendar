using System.Net;
using System.Net.Http.Json;
using GameEventApi.Contracts;

namespace GameEventApi.Tests;

public class EventTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("netrunner", "Draft", null)] // Draft is a Magic format, not Netrunner
    [InlineData("dnd", "One-shot", 12)]      // above the D&D template's max of 7
    [InlineData("chess", "Blitz", null)]     // no such template
    public async Task Create_rejects_values_the_template_does_not_allow(string game, string format, int? capacity)
    {
        var response = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            "Test event", game, format, DateTimeOffset.UtcNow.AddDays(1), capacity));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_applies_template_defaults()
    {
        var response = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            "Netrunner Night", "netrunner", "Startup", DateTimeOffset.UtcNow.AddDays(1), null));
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(12, ev!.Capacity);
        Assert.Equal(240, ev.DurationMinutes);
        Assert.Equal(4, ev.MinPlayers);
    }

    [Fact]
    public async Task Invite_has_title_times_and_location()
    {
        var startsAt = new DateTimeOffset(DateTime.UtcNow.Year + 1, 3, 14, 19, 0, 0, TimeSpan.FromHours(-7));
        var created = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            "Friday Night Magic", "mtg", "Draft", startsAt, null));
        var ev = await created.Content.ReadFromJsonAsync<EventDto>();

        var response = await _client.GetAsync($"/api/events/{ev!.Id}/invite.ics");
        var ics = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/calendar", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("SUMMARY:Friday Night Magic", ics);
        Assert.Contains($"DTSTART:{startsAt.UtcDateTime:yyyyMMdd'T'HHmmss}Z", ics);
        Assert.Contains($"DTEND:{startsAt.UtcDateTime.AddMinutes(240):yyyyMMdd'T'HHmmss}Z", ics);
        Assert.Contains("LOCATION:123 Example St\\, Renton\\, WA 98057", ics);
    }
}
