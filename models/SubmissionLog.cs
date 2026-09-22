namespace LeetPOTD.Models;

public sealed class SubmissionLog
{
    public int Id { get; set; }
    public int PotdRunId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ExecutionStatus Status { get; set; }
    public required string Step { get; set; }
    public string? Message { get; set; }
    public string? SubmissionId { get; set; }
}
