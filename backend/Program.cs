using Microsoft.EntityFrameworkCore;
using Upiter.Api.Data;
using Upiter.Api.Models;
using Upiter.Api.Services;
using Upiter.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UpiterDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
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

app.Run();

