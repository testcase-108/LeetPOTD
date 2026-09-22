using LeetPOTD.Models;

namespace LeetPOTD.Services;

public interface IPotdOrchestrator
{
    Task<PotdRun> RunAsync(CancellationToken cancellationToken = default);
}
