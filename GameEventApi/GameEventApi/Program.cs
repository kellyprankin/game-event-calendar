using GameEventApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

app.Run();
