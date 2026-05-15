namespace Wander.Api.Domain;

public class AdminAuditLog
{
    public long Id { get; set; }
    public required string EventType { get; set; }
    public string Severity { get; set; } = "info";
    public string? ActorId { get; set; }
    public string? ActorUsername { get; set; }
    public string? TargetId { get; set; }
    public string? TargetUsername { get; set; }
    public string? TargetType { get; set; }
    public int? AffectedCount { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
