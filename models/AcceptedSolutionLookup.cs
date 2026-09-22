namespace LeetPOTD.Models;

public sealed class AcceptedSolutionLookup
{
    public LocalSolution? Solution { get; init; }
    public required string Message { get; init; }
}
