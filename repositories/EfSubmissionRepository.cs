using LeetPOTD.Data;
using LeetPOTD.Models;

namespace LeetPOTD.Repositories;

public sealed class EfSubmissionRepository(LeetPotdDbContext dbContext) : ISubmissionRepository
{
    public async Task SaveLogAsync(SubmissionLog log, CancellationToken cancellationToken = default)
    {
        dbContext.SubmissionLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
