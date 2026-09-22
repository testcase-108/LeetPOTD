using LeetPOTD.Models;
using LeetPOTD.Repositories;

namespace LeetPOTD.Services;

public sealed class PotdOrchestrator(
    ILeetCodeService leetCode,
    IPotdRepository potdRepository,
    ILogger<PotdOrchestrator> logger) : IPotdOrchestrator
{
    public async Task<PotdRun> RunAsync(CancellationToken cancellationToken = default)
    {
        var run = new PotdRun
        {
            StartedAt = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Started
        };

        AddLog(run, ExecutionStatus.Started, "RunStarted", "POTD run started.");

        try
        {
            var problem = await leetCode.GetDailyProblemAsync(cancellationToken);

            run.ProblemTitle = problem.Title;
            run.ProblemSlug = problem.Slug;
            run.ProblemUrl = problem.Url;
            run.Status = ExecutionStatus.DailyProblemResolved;
            AddLog(run, run.Status, "GetDailyProblem", $"Resolved daily problem: {problem.Title}.");

            var solution = await leetCode.FindLocalSolutionAsync(problem, cancellationToken);
            if (solution is null)
            {
                AddLog(
                    run,
                    ExecutionStatus.NoSolutionFound,
                    "FindLocalSolution",
                    "No local solution file matched the current POTD.");

                var acceptedSolutionLookup = await leetCode.FindAcceptedSolutionAsync(problem, cancellationToken);
                AddLog(
                    run,
                    acceptedSolutionLookup.Solution is null
                        ? ExecutionStatus.NoSolutionFound
                        : ExecutionStatus.SolutionLocated,
                    "FindAcceptedSolution",
                    acceptedSolutionLookup.Message);

                solution = acceptedSolutionLookup.Solution;

                if (solution is null)
                {
                    run.Status = ExecutionStatus.NoSolutionFound;
                    run.FinishedAt = DateTimeOffset.UtcNow;
                    run.Summary = "No local solution or accepted LeetCode submission matched the current POTD.";
                    await potdRepository.SaveRunAsync(run, cancellationToken);
                    return run;
                }
            }

            run.SolutionPath = solution.FilePath;
            run.Status = ExecutionStatus.SolutionLocated;
            AddLog(run, run.Status, "FindSolution", $"Found solution: {solution.FilePath}.");

            if (solution.IsAcceptedLeetCodeSubmission)
            {
                run.Status = ExecutionStatus.Accepted;
                AddLog(
                    run,
                    run.Status,
                    "AcceptedSolutionFound",
                    "Found an accepted LeetCode submission and stored it locally. No re-submit needed.");

                run.Status = ExecutionStatus.Completed;
                run.FinishedAt = DateTimeOffset.UtcNow;
                run.Summary = "Accepted LeetCode solution downloaded and saved locally.";
                AddLog(run, run.Status, "RunComplete", run.Summary);

                await potdRepository.SaveRunAsync(run, cancellationToken);
                return run;
            }

            run.Attempts++;
            var submission = await leetCode.SubmitSolutionAsync(problem, solution, cancellationToken);

            run.Status = submission.Status;
            AddLog(
                run,
                submission.Status,
                "SubmitSolution",
                submission.Message ?? $"Submission completed with status {submission.Status}.",
                submission.SubmissionId);

            if (!submission.IsAccepted)
            {
                run.FinishedAt = DateTimeOffset.UtcNow;
                run.Summary = "Submission was not accepted. Gemini fallback is intentionally left behind its own future integration.";
                await potdRepository.SaveRunAsync(run, cancellationToken);
                return run;
            }

            run.Status = ExecutionStatus.Completed;
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.Summary = "POTD submission accepted.";
            AddLog(run, run.Status, "RunComplete", run.Summary);

            await potdRepository.SaveRunAsync(run, cancellationToken);
            return run;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "POTD run failed.");

            run.Status = ExecutionStatus.Error;
            run.FinishedAt = DateTimeOffset.UtcNow;
            run.Summary = ex.Message;
            AddLog(run, run.Status, "RunFailed", ex.Message);

            await potdRepository.SaveRunAsync(run, CancellationToken.None);
            return run;
        }
    }

    private static void AddLog(
        PotdRun run,
        ExecutionStatus status,
        string step,
        string? message,
        string? submissionId = null)
    {
        run.Logs.Add(new SubmissionLog
        {
            Status = status,
            Step = step,
            Message = message,
            SubmissionId = submissionId
        });
    }
}
