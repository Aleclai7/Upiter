using Microsoft.EntityFrameworkCore;
using Upiter.Api.Data;
using Upiter.Api.Models;
using Upiter.Api.Services;
using Upiter.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UpiterDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<EmailAlertService>();
builder.Services.AddHostedService<UptimeServiceChecker>();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.MapHub<StatusHub>("/hubs/status");

app.MapGet("/targets", async (UpiterDbContext db) =>
{
    var targets = await db.Targets.ToListAsync();
    return Results.Ok(targets);
});

app.MapPost("/targets", async (Target target, UpiterDbContext db) =>
{
    db.Targets.Add(target);
    await db.SaveChangesAsync();
    return Results.Created($"/targets/{target.Id}", target);
});

app.MapGet("/targets/{id}", async (int id, UpiterDbContext db) =>
{
    var target = await db.Targets.FindAsync(id);
    return target is not null ? Results.Ok(target) : Results.NotFound();
});

app.MapPatch("/targets/{id}", async (int id, Target updated, UpiterDbContext db) =>
{
    var target = await db.Targets.FindAsync(id);
    if (target is null) return Results.NotFound();

    target.Name = updated.Name;
    target.Url = updated.Url;
    target.IntervalSeconds = updated.IntervalSeconds;
    target.IsActive = updated.IsActive;

    await db.SaveChangesAsync();
    return Results.Ok(target);
});

app.MapDelete("/targets/{id}", async (int id, UpiterDbContext db) =>
{
    var target = await db.Targets.FindAsync(id);
    if (target is null) return Results.NotFound();

    db.Targets.Remove(target);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/history/{targetId}", async (int targetId, int? limit, UpiterDbContext db) =>
{
  var targetExists = await db.Targets.AnyAsync(t => t.Id == targetId);
  if (!targetExists) return Results.NotFound();

    var take = Math.Clamp(limit ?? 100, 1, 1000);

    var history = await db.CheckResults
      .Where(r => r.TargetId == targetId)
      .OrderByDescending(r => r.CheckedAt)
      .Take(take)
      .Select(r => new
      {
          r.Id,
          r.CheckedAt,
          r.IsUp,
          r.StatusCode,
          r.ResponseTimeMs,
          r.ErrorMessage
          })
      .ToListAsync();

    return Results.Ok(history);
});

app.MapGet("/status", async (UpiterDbContext db) =>
  {
    var since = DateTime.UtcNow.AddHours(-24);

    var rows = await db.Targets
        .Select(t => new
        {
            t.Id,
            t.Name,
            t.Url,
            t.IntervalSeconds,
            t.IsActive,
            t.LastKnownUp,
            t.LastCheckedAt,
            TotalChecks = t.CheckResults.Count(r => r.CheckedAt >= since),
            UpChecks = t.CheckResults.Count(r => r.CheckedAt >= since && r.IsUp)
        })
        .ToListAsync();

    var statuses = rows.Select(r => new
    {
        r.Id,
        r.Name,
        r.Url,
        r.IntervalSeconds,
        r.IsActive,
        r.LastKnownUp,
        r.LastCheckedAt,
        UptimePercent24h = r.TotalChecks == 0
            ? (double?)null
            : Math.Round(100.0 * r.UpChecks / r.TotalChecks, 2)
    });

  return Results.Ok(statuses);
});

app.Run();

