using LeetPOTD.Models;

namespace LeetPOTD.Repositories;

public interface ISubmissionRepository
{
    Task SaveLogAsync(SubmissionLog log, CancellationToken cancellationToken = default);
}
