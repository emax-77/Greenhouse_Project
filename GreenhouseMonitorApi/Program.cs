using System.Text.Encodings.Web;
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

app.MapGet("/api/greenhouse/readings/latest", async (GreenhouseContext db, HttpContext http) =>
{
    var latest = await db.Readings
        .OrderByDescending(r => r.Timestamp)
        .FirstOrDefaultAsync();

    if (latest is null)
    {
        return Results.NotFound();
    }

    var acceptHeader = http.Request.Headers.Accept.ToString();
    if (acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        var escapedDeviceId = HtmlEncoder.Default.Encode(latest.DeviceId);
        var formattedTime = HtmlEncoder.Default.Encode(latest.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        var html = $@"
<!DOCTYPE html>
<html lang=""sk"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Posledné meranie – Greenhouse Monitor</title>
  <style>
    body {{ font-family: Arial, sans-serif; margin: 2rem; background: #f3f7fb; color: #111827; }}
    .card {{ max-width: 720px; margin: 0 auto; background: #ffffff; border-radius: 16px; padding: 1.5rem 2rem; box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08); }}
    h1 {{ margin-top: 0; }}
    .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-top: 1.5rem; }}
    .metric {{ border: 1px solid #dbeafe; background: #eff6ff; border-radius: 12px; padding: 1rem; }}
    .metric strong {{ display: block; font-size: 1.4rem; margin-top: 0.5rem; }}
    .meta {{ color: #475569; margin-top: 1rem; }}
  </style>
</head>
<body>
  <div class=""card"">
    <h1>Posledné meranie</h1>
    <p class=""meta"">Zariadenie: {escapedDeviceId}</p>
    <p class=""meta"">Čas: {formattedTime}</p>
    <div class=""grid"">
      <div class=""metric"">Teplota <strong>{latest.Temperature:0.00} °C</strong></div>
      <div class=""metric"">Vlhkosť <strong>{latest.Humidity:0.00} %</strong></div>
      <div class=""metric"">Tlak <strong>{latest.Pressure:0.00} hPa</strong></div>
    </div>
  </div>
</body>
</html>";

        return Results.Content(html, "text/html; charset=utf-8");
    }

    return Results.Ok(latest);
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
