using Microsoft.EntityFrameworkCore;
using Upiter.Api.Data;
using Upiter.Api.Models;
using Upiter.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UpiterDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddHostedService<UptimeServiceChecker>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.Run();

