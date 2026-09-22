using LeetPOTD.Models;

namespace LeetPOTD.Services;

public interface ILeetCodeService
{
    Task<PotdProblem> GetDailyProblemAsync(CancellationToken cancellationToken = default);
    Task<LocalSolution?> FindLocalSolutionAsync(PotdProblem problem, CancellationToken cancellationToken = default);
    Task<AcceptedSolutionLookup> FindAcceptedSolutionAsync(PotdProblem problem, CancellationToken cancellationToken = default);
    Task<SubmissionResult> SubmitSolutionAsync(PotdProblem problem, LocalSolution solution, CancellationToken cancellationToken = default);
    Task<SubmissionResult> GetSubmissionStatusAsync(string submissionId, CancellationToken cancellationToken = default);
}
