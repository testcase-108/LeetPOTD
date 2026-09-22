namespace LeetPOTD.Models;

public sealed class LocalSolution
{
    public required string FilePath { get; init; }
    public required string Language { get; init; }
    public required string SourceCode { get; init; }
    public bool IsAcceptedLeetCodeSubmission { get; init; }
}
