using GameEventApi.Data;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace GameEventApi.Services;

public static class CalendarInvite
{
    // Times are written in UTC; Google Calendar and Outlook convert them to the viewer's zone.
    public static string Create(Event ev, GameTemplate template, StoreOptions store, DateTime nowUtc)
    {
        var start = DateTime.SpecifyKind(ev.StartsAtUtc, DateTimeKind.Utc);
        var calendarEvent = new CalendarEvent
        {
            Uid = $"{ev.Id}@gameevent",
            Summary = ev.Name,
            Description = $"{template.Name}: {ev.Format}",
            Location = store.Location,
            DtStart = new CalDateTime(start),
            DtEnd = new CalDateTime(start.AddMinutes(ev.DurationMinutes)),
            DtStamp = new CalDateTime(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc)),
        };

        var calendar = new Calendar { ProductId = "-//GameEvent//Event Calendar//EN" };
        calendar.AddProperty("METHOD", "PUBLISH");
        calendar.Events.Add(calendarEvent);
        return new CalendarSerializer().SerializeToString(calendar)!;
    }
}
