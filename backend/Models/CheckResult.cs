namespace Upiter.Api.Models;

public class CheckResult
{
    public int Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; } 

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsUp { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}