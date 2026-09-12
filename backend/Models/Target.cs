namespace Upiter.Api.Models;

public class Target
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool? LastKnownUp { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public List<CheckResult> CheckResults { get; set; } = new();
}