using LeetPOTD.Data;
using LeetPOTD.Models;

namespace LeetPOTD.Repositories;

public sealed class EfPotdRepository(LeetPotdDbContext dbContext) : IPotdRepository
{
    public async Task SaveRunAsync(PotdRun run, CancellationToken cancellationToken = default)
    {
        dbContext.PotdRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
