using Microsoft.EntityFrameworkCore;
using Upiter.Api.Data;
using Upiter.Api.Models;
using Upiter.Api.Services;

namespace Upiter.Api.Services;

public class UptimeServiceChecker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UptimeServiceChecker> _logger;

    public UptimeServiceChecker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<UptimeServiceChecker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckTargetsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task CheckTargetsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UpiterDbContext>();

        var now = DateTime.UtcNow;

        var dueTargets = await db.Targets
            .Where(t => t.IsActive)
            .Where(t => t.LastCheckedAt == null ||
                        now >= t.LastCheckedAt.Value.AddSeconds(t.IntervalSeconds))
            .ToListAsync(stoppingToken);

        foreach (var target in dueTargets)
        {
            await PingTargetAsync(target, db, stoppingToken);
        }
    }

    private async Task PingTargetAsync(Target target, UpiterDbContext db, CancellationToken stoppingToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var result = new CheckResult
        {
            TargetId = target.Id,
            CheckedAt = DateTime.UtcNow
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await httpClient.GetAsync(target.Url, stoppingToken);
            stopwatch.Stop();

            result.IsUp = response.IsSuccessStatusCode;
            result.StatusCode = (int)response.StatusCode;
            result.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsUp = false;
            result.ErrorMessage = ex.Message;
        }

        bool statusChanged = target.LastKnownUp.HasValue && target.LastKnownUp.Value != result.IsUp;

        target.LastKnownUp = result.IsUp;
        target.LastCheckedAt = result.CheckedAt;

        db.CheckResults.Add(result);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Checked {Name}: {Status}", target.Name, result.IsUp ? "UP" : "DOWN");

        if (statusChanged)
        {
            _logger.LogWarning("STATUS CHANGED for {Name}: now {Status}", target.Name, result.IsUp ? "UP" : "DOWN");
            // SignalR broadcast + Telegram alert will go here — next steps
        }
    }
}
