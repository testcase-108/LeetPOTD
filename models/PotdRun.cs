namespace LeetPOTD.Models;

public sealed class PotdRun
{
    public int Id { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Started;
    public string? ProblemTitle { get; set; }
    public string? ProblemSlug { get; set; }
    public string? ProblemUrl { get; set; }
    public string? SolutionPath { get; set; }
    public int Attempts { get; set; }
    public string? Summary { get; set; }
    public List<SubmissionLog> Logs { get; set; } = [];
}
