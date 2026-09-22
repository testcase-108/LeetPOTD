using LeetPOTD.Models;

namespace LeetPOTD.Repositories;

public interface IPotdRepository
{
    Task SaveRunAsync(PotdRun run, CancellationToken cancellationToken = default);
}
