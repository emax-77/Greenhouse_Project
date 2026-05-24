using GreenhouseMonitorApi.Data;
using GreenhouseMonitorApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GreenhouseContext>(options =>
    options.UseSqlite("Data Source=greenhouse.db"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string apiKey = "greenhouse_local_key_2026";

app.MapPost("/api/greenhouse/readings", async (Reading reading, HttpContext http, GreenhouseContext db) =>
{
    if (!http.Request.Headers.TryGetValue("x-api-key", out var providedKey) || providedKey != apiKey)
    {
        return Results.Unauthorized();
    }

    reading.Timestamp = DateTime.UtcNow;
    db.Readings.Add(reading);
    await db.SaveChangesAsync();
    return Results.Created($"/api/greenhouse/readings/{reading.Id}", reading);
});

app.MapGet("/api/greenhouse/readings/latest", async (GreenhouseContext db) =>
{
    var latest = await db.Readings
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync();

    return latest is not null ? Results.Ok(latest) : Results.NotFound();
});

app.MapGet("/api/greenhouse/readings", async (GreenhouseContext db) =>
{
    var list = await db.Readings
        .OrderByDescending(r => r.Timestamp)
        .Take(100)
        .ToListAsync();
    return Results.Ok(list);
});

app.MapGet("/api/greenhouse/health", () => Results.Ok(new { status = "running" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GreenhouseContext>();
    db.Database.EnsureCreated();
}

app.Run();
