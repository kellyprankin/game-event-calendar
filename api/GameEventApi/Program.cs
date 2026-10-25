using GameEventApi;
using GameEventApi.Data;
using GameEventApi.Endpoints;
using GameEventApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddSingleton<RegistrationLinks>();
builder.Services.Configure<StoreOptions>(builder.Configuration.GetSection(StoreOptions.Section));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSeeding((db, _) => SeedData.SeedTemplates(db))
        .UseAsyncSeeding((db, _, ct) => SeedData.SeedTemplatesAsync(db, ct)));

var app = builder.Build();

// Apply migrations on startup so `docker compose up` is the only setup step.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var api = app.MapGroup("/api");
api.MapTemplateEndpoints();
api.MapEventEndpoints();
api.MapRegistrationEndpoints();

// Unknown /api paths stay 404s; any other path loads the Vue app, which handles routing.
api.MapFallback(() => TypedResults.NotFound());
// no-cache so browsers always pick up the latest build; the hashed asset files can still be cached.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

app.Run();
