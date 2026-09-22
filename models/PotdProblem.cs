namespace LeetPOTD.Models;

public sealed class PotdProblem
{
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Difficulty { get; init; }
    public string? Url { get; init; }
    public string? Statement { get; init; }
    public DateOnly? Date { get; init; }
}
