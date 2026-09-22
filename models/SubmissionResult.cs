namespace LeetPOTD.Models;

public sealed class SubmissionResult
{
    public string? SubmissionId { get; init; }
    public ExecutionStatus Status { get; init; }
    public bool IsAccepted => Status == ExecutionStatus.Accepted;
    public string? Message { get; init; }
    public string? Runtime { get; init; }
    public string? Memory { get; init; }
}
