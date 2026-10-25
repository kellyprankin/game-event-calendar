using GameEventApi.Contracts;
using GameEventApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GameEventApi.Endpoints;

public static class TemplateEndpoints
{
    public static RouteGroupBuilder MapTemplateEndpoints(this RouteGroupBuilder api)
    {
        api.MapGroup("/templates").WithTags("Templates")
            .MapGet("/", ListTemplates);

        return api;
    }

    static async Task<Ok<List<TemplateDto>>> ListTemplates(AppDbContext db, CancellationToken ct)
    {
        var templates = await db.GameTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return TypedResults.Ok(templates.Select(TemplateDto.From).ToList());
    }
}
